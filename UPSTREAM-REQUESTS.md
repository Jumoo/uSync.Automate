# Upstream API requests

Things discovered while building `uSync.Automate` (settings sync) and `uSync.Automate.Actions`
(workflow actions/triggers) that would be better served by a small change in Umbraco.Automate or
uSync itself, rather than worked around here. Ranked roughly by how much they currently cost us.

## Umbraco.Automate

1. **[Bug, affects built-in triggers too] Loop-protection check is skipped when a trigger's settings are empty.**
   `Dispatch/TriggerEventHandler.cs` only evaluates `IAutomationOriginatedEventBehavior` inside
   `if (trigger.SettingsType is not null && triggerSettings is { Count: > 0 })`. An automation whose
   trigger has no persisted settings dictionary gets **no loop protection at all** - this affects
   Automate's own built-in content triggers, not just ours. Every settings class we ship gives
   itself a non-empty default (a belt-and-braces workaround, not a fix) and we've added a
   regression test asserting the check actually fires. Fix: hoist the check out of that guard,
   falling back to a default-constructed settings instance when none is persisted.

2. **`AutomationUnpublishedNotification` has no public accessor for its target automation.**
   Every other lifecycle notification (`AutomationSaved`, `AutomationPublished`, `AutomationDeleted`,
   `WorkspaceSaved`, `WorkspaceGroupSaved`, ...) exposes its target via a named property (`.Automation`,
   `.Workspace`, ...). `AutomationUnpublishedNotification`'s declaration has no body at all:
   ```csharp
   public sealed class AutomationUnpublishedNotification(Automation target, EventMessages messages)
       : ObjectNotification<Automation>(target, messages);
   ```
   `uSync.Automate`'s `AutomationHandler` can react to `AutomationPublishedNotification` (rewriting
   the file's `<Status>`) but has no way to react to the matching unpublish event - it currently
   relies on the next scheduled/manual export to pick up the change. A one-line fix (add
   `public Automation Automation { get; } = target;`, matching every sibling class) would close
   this. Non-breaking.

3. **The four `*Saving`/`*Deleting` notifications expose no public accessor for their target either**
   (`AutomationSavingNotification`, `AutomationDeletingNotification`, `WorkspaceSavingNotification`,
   etc. all derive from `CancelableObjectNotification<T>`, whose `Target` is `protected`). uSync's
   own root-lock feature (`SyncHandlerRoot.ShouldBlockRootChangesAsync`) needs the entity to compute
   a file path before deciding whether to block a change - without a public accessor, we can't wire
   root-lock support for Automate items at all. Same fix as #2: a public property alongside `Target`.

4. **`CreateAutomationAsync` and `WorkspaceGroupService`'s save methods use the synchronous
   `PublishCancelable`, while `UpdateAutomationAsync` uses `PublishCancelableAsync`.** An
   `INotificationAsyncHandler<AutomationSavingNotification>` (or the workspace-group equivalent)
   silently never fires on the *create* path. Independent of anything we're doing - flagging it as
   a latent bug we noticed while wiring notification handlers.

5. **No public API for a third-party action to resume a suspended step.** `ActionResult.WaitForInput`
   exists, but resuming it is entirely internal (`ActionStepBody.HandleResumeAsync`, driven by the
   built-in approval flow). This blocks any "kick off long work, resume on completion" action design
   - we ruled it out for `uSync.import` for exactly this reason and run synchronously instead. A
   public `IAutomationSuspensionResolver.ResumeAsync(eventName, eventKey, data, ct)` would unblock it.

6. **No in-process hook to suppress a trigger for a specific notification.** A uSync import raises
   thousands of Umbraco `ContentSavedNotification`s tagged internally with
   `State[uSync.EventStateKey] = true`, but Automate's built-in `ContentSavedTrigger.MapEvent` has
   no way to see that tag, and there's no extension point between the notification and
   `TriggerEventHandler.DispatchAsync` where a third party could intervene. Today the only
   mitigation is telling operators to set `OnAutomationOriginatedEvent = SkipAlways` on content
   triggers on sites that also run uSync imports (documented in `uSync.Automate.Actions`' readme).
   An `ITriggerEventSuppressor` collection, consulted in `TriggerNotificationHandler` before dispatch
   and given the raw notification, would let a package like ours suppress the cascade itself instead
   of relying on manual configuration.

7. **`IdempotencyKeyFactory` is internal.** Every third-party trigger that wants batch-style dedupe
   has to reimplement the same SHA-256-of-sorted-keys shape `NotificationTriggerBase`'s protected
   `GenerateIdempotencyKey` helpers already build internally. Exposing the batch-hash helper (or
   making the whole factory `protected` on the base class) would remove the duplication.

8. **`AutomationExportDefinition` (the Transfer/Deploy JSON contract) carries no `GroupId` or
   `Status`.** Not a blocker for us - we deliberately don't use the Transfer envelope (see the
   `uSync.Automate` readme for why) - but worth fixing if the Deploy connector is ever expected to
   round-trip group placement or publish state.

9. **`AutomationService.CurrentExportFormatVersion` is an `internal const`** compared with strict
   `!=` in `ValidateFormatVersion`. A public accessor (or a `SupportsFormatVersion(string)` predicate)
   would let a third party emit a compatible envelope without hardcoding "1.0" and hoping it doesn't
   change.

## uSync

1. **Item-level notifications don't carry the handler.** `SyncHandlerRoot.cs` fires
   `uSyncImportedItemNotification`/`uSyncExportedItemNotification`/`uSyncReportedItemNotification`
   via the `(item, change)` constructor everywhere; the `(item, handler)` overload exists on
   `uSyncItemNotification<TObject>` but is never used for these three. As a result,
   `uSync.itemImported`/`uSync.itemExported` in `uSync.Automate.Actions` can filter by XML item
   type but **not** by handler alias - a real usability gap for anyone who only cares about, say,
   the content handler's imports. Passing the handler through at each of those three call sites
   would fix it without any API shape change.

2. **No `CancellationToken` anywhere in the `ISyncActionService`/`uSyncManagementService`
   orchestration chain.** `uSyncRunner` (in `uSync.Automate.Actions`) can only check a token
   *between* handlers - once `ExportHandlerAsync`/`ImportHandlerAsync`/`ReportHandlerAsync` starts
   for a given handler, there's no way to interrupt it. Threading a `CancellationToken` through
   these methods (even as an optional, default-`None` parameter to stay source-compatible) would
   let a long-running workflow action actually respect Automate's own step cancellation.

3. **No public "run a whole group" convenience.** `ISyncActionService` only exposes one handler at
   a time; the start → per-handler loop → post → finish sequence lives in
   `uSyncManagementService.PerformActionInternalAsync`, which is internal to the management API
   project. We had to reimplement that ~40-line loop in `uSyncRunner` from scratch, reading the
   management service's source as a reference. A public
   `Task<SyncActionResult> ISyncActionService.RunAsync(HandlerActions, SyncActionOptions, uSyncCallbacks?)`
   that does the whole sequence would remove this duplication for us and any future integration.

4. **`SyncActionOptions.Folders` isn't validated against uSync's working-folder root.**
   `SyncActionService`'s upload path has a private `MakeValidImportFolder` containment check, but
   it's never applied to the `Folders` array on `SyncActionOptions` itself - a caller (including
   our `Folders` action setting) can point an import or export anywhere on disk the process can
   write to. We mitigate with an opt-in `AllowCustomFolders` flag defaulting off, but a public
   `ISyncFileService.IsFolderAllowed(string)` (or validating inside `SyncActionService` itself)
   would close this at the source for every caller, not just ours.

5. **No way to attribute an automation-driven import/export to a specific user.**
   `SyncActionOptions` has no `UserId`/`Username` field, so `SyncActionService.ImportHandlerAsync`
   (etc.) always builds its internal `uSyncImportOptions` with `UserId = -1`. We can attribute the
   run in Automate's own audit trail (`AutomationExecutionContext.FormatPerformingDetails()`,
   surfaced via `SyncStartActionRequest.Username`), but uSync's own serializer-level "who changed
   this" stays "system" for every automation-driven run. Adding `UserId`/`Username` to
   `SyncActionOptions`, plumbed into `uSyncImportOptions.UserId`, would fix the discrepancy.

6. **`uSyncExportCompletedNotification.Group` is inconsistently populated.** The startup-export
   path (`SyncService.ExportAsync`) always fires it with `group: null`; `FinishBulkProcessAsync`
   passes the real group. `uSync.exportCompleted`'s `Group` filter in `uSync.Automate.Actions` is
   therefore unreliable depending on how the export was triggered - worth making consistent.

## uSync.Complete / Jumoo.Processing.Core

Discovered building `uSync.Automate.Actions.Complete` (publisher push/pull and restore point
actions/triggers).

1. **`PublisherStrategy` (pipeline/strategy alias constants) is `internal` to `uSync.Publisher`.**
   A third-party integration driving `IPipelineService` directly - the only way to run a push/pull
   headlessly, see `uSync.Complete.Hangfire`'s `ProcessingScheduler` - has no choice but to
   re-declare `"PublisherPipeline"`/`"RealtimePushStrategy"`/`"RealtimePullStrategy"` as string
   literals (`CompletePipelines.cs` in this package). Making the class `public` (it's already the
   shape `uSyncRestorePoints` uses publicly for the same purpose) would remove the duplication and
   the risk of it silently drifting out of sync.

2. **`ProcessingCompletedNotification` (`Jumoo.Processing.Core`) carries only a `RequestId` -
   no strategy or pipeline alias.** This makes it impossible to build a "restore point finished"
   trigger without a fragile in-memory `RequestId` → strategy correlation map populated by
   whichever code started the pipeline (and lost across an app pool recycle). We shipped a
   `uSyncComplete.publishReceived` trigger against `uSyncPublisherCompletedNotification` (which
   does carry enough context, via `PublisherActionRequest`) but had to leave restore-point
   completion untriggerable for the same reason. Adding a `Strategy`/`PipelineAlias` property to
   `ProcessingNotificationBase` would unblock a generic "any pipeline completed" trigger instead
   of one per feature.

3. **`SyncPublisherPipelineProcessor.UpdateOptions` silently overwrites the flags on every
   `SyncItem`** it's given, deriving them from `PublisherProcessingOptions.PublisherOptions`
   instead. Not a bug exactly, but undocumented behaviour that `uSync.Complete.Hangfire`'s
   `PublisherScheduler` gets wrong (it sets flags on the `SyncItem` and they're discarded). A
   doc-comment on `PublisherProcessingOptions.Items`/`PublisherOptions` noting which one wins
   would have saved the archaeology.

## What we didn't ask for

`Umbraco.Deploy.Automate` has an `InternalsVisibleTo` grant into `Umbraco.Automate.Core` that we
don't have and aren't requesting - our design deliberately stays on the public surface (see the
"why we don't use the Transfer envelope" section of `uSync.Automate`'s readme), so none of the
above needs an internals grant to resolve.
