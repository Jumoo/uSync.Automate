# uSync.Automate.Actions.Complete

Umbraco.Automate actions and triggers for uSync.Complete:

- **Actions** - push or pull content/media to another server, by item key, from a dropdown of
  the two supported entity types (`uSyncComplete.push`, `uSyncComplete.pull`); push or pull a
  settings item - document type, data type, template, member, or a third-party entity type - by
  UDI entity type and key (`uSyncComplete.pushSettings`, `uSyncComplete.pullSettings`); or take a
  restore point (`uSyncComplete.createRestorePoint`).
- **Triggers** - start a workflow when this site finishes receiving a push/pull from another
  server (`uSyncComplete.publishReceived`).

Requires a licensed uSync.Complete (specifically `uSync.Publisher` and `uSync.Expansions.Core`)
alongside [uSync.Automate.Actions](https://www.nuget.org/packages/uSync.Automate.Actions) - this
package depends on it for the shared operation gate and the "triggered by another automation"
setting, and won't do anything useful without uSync.Complete installed too.

## Blocking behaviour

Every action in this package runs **inline** - the workflow step blocks until the pipeline
finishes. A push of a large content tree can take minutes; size your workflow's step timeout
accordingly.

## The trigger only fires on the receiving side

`uSyncComplete.publishReceived` wraps uSync.Complete's own completion notification, which is
only raised by the server *receiving* a push or pull, once it's finished applying it. The
sending side doesn't get this trigger - it already has the outcome directly, as the result of
the `uSyncComplete.push`/`uSyncComplete.pull` action that started the operation.

## Loop protection

Applying an incoming push/pull can fire thousands of Umbraco content-saved notifications, which
can in turn trigger Automate content automations. If those automations call back into a push/pull
of their own, the two servers can cascade into each other. This package guards its own action
against a concurrent local uSync operation (sharing the gate uSync.Automate.Actions uses for
local imports/exports), and its trigger defaults to skipping automation-originated events - see
uSync.Automate.Actions' own readme for the wider loop-protection guidance and its limits.

## License

MPL-2.0
