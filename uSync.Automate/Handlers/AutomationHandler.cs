using Microsoft.Extensions.Logging;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Notifications;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;
using uSync.Automate.Notifications;
using uSync.Automate.Services;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

namespace uSync.Automate.Handlers;

/// <summary>
/// Syncs Automate <see cref="Automation"/> definitions. Two-pass: the first sweep across all
/// handlers creates/updates every automation's definition (workspace, group, trigger, steps);
/// the second sweep applies <see cref="AutomationStatus"/> via
/// <see cref="AutomationSerializer"/>'s <c>DeserializeSecondPassAsync</c>, so publishing runs
/// only once every automation, workspace and group in the import already exists.
/// </summary>
[SyncHandler(
    "automationHandler",
    "Automations",
    "Automate-Automations",
    uSyncAutomatePriorities.Automations,
    Icon = "icon-fa-robot",
    IsTwoPass = true,
    EntityType = uSyncAutomate.AutomationEntityType)]
public class AutomationHandler : SyncHandlerRoot<Automation, Automation>, ISyncHandler,
    INotificationAsyncHandler<AutomationSavedNotification>,
    INotificationAsyncHandler<AutomationDeletedNotification>,
    INotificationAsyncHandler<AutomationPublishedNotification>
{
    private readonly SyncAutomateService _automateService;

    public override string Group => uSyncAutomate.GroupName;

    public AutomationHandler(
        ILogger<SyncHandlerRoot<Automation, Automation>> logger,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        ISyncItemFactory itemFactory,
        SyncAutomateService automateService)
        : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
    {
        _automateService = automateService;
    }

    protected override Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(Automation parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
        => Task.FromResult(Enumerable.Empty<uSyncAction>());

    protected override async Task<IEnumerable<Automation>> GetChildItemsAsync(Automation? parent)
        => parent is null ? await _automateService.GetAllAutomationsAsync() : [];

    protected override Task<IEnumerable<Automation>> GetFoldersAsync(Automation? parent)
        => Task.FromResult(Enumerable.Empty<Automation>());

    protected override Task<Automation?> GetFromServiceAsync(Automation? item)
        => item is null ? Task.FromResult<Automation?>(null) : _automateService.GetAutomationAsync(item.Id);

    protected override string GetItemName(Automation item) => item.Name;

    protected override string GetItemPath(Automation item, bool useGuid, bool isFlat)
        => useGuid ? item.Id.ToString() : item.Alias.ToSafeFileName(shortStringHelper);

    public Task HandleAsync(AutomationSavedNotification notification, CancellationToken cancellationToken)
        => HandleAsync(new AutomateSavedNotification<Automation>(notification.Automation, notification.Messages), cancellationToken);

    public Task HandleAsync(AutomationDeletedNotification notification, CancellationToken cancellationToken)
        => HandleAsync(new AutomateDeletedNotification<Automation>(notification.Automation, notification.Messages), cancellationToken);

    // Publishing changes <Status> in the file, so it re-exports via the same "saved" path.
    // AutomationUnpublishedNotification has no public accessor for its target automation
    // (unlike every other Automate notification, its sealed declaration has no body) so it
    // can't be forwarded here - see the uSync.Automate readme / upstream Automate API asks.
    // Unpublishing a previously-published automation still gets picked up by the next export.
    public Task HandleAsync(AutomationPublishedNotification notification, CancellationToken cancellationToken)
        => HandleAsync(new AutomateSavedNotification<Automation>(notification.Automation, notification.Messages), cancellationToken);
}
