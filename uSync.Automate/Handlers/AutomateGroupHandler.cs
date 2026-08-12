using Microsoft.Extensions.Logging;
using Umbraco.Automate.Core.Notifications;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Strings;
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
/// Syncs Automate <see cref="WorkspaceGroup"/>s. Overrides <see cref="GetMergedItemsAsync"/> to
/// sort by the XML <c>Level</c> attribute so parents always import before children - without
/// this, Automate's own <c>WorkspaceGroupService.ValidateParentAsync</c> throws on a deep tree
/// imported out of order. Cloned from uSync.Forms' <c>FormsFolderHandler</c>, which solves the
/// same ordering problem for its own hierarchy type.
/// </summary>
[SyncHandler(
    "automateGroupHandler",
    "Automate Groups",
    "Automate-Groups",
    uSyncAutomatePriorities.Groups,
    Icon = "icon-folder",
    EntityType = uSyncAutomate.GroupEntityType)]
public class AutomateGroupHandler : SyncHandlerRoot<WorkspaceGroup, WorkspaceGroup>, ISyncHandler,
    INotificationAsyncHandler<WorkspaceGroupSavedNotification>,
    INotificationAsyncHandler<WorkspaceGroupDeletedNotification>
{
    private readonly SyncAutomateService _automateService;

    public override string Group => uSyncAutomate.GroupName;

    public AutomateGroupHandler(
        ILogger<SyncHandlerRoot<WorkspaceGroup, WorkspaceGroup>> logger,
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
        ItemContainerType = Umbraco.Cms.Core.Models.UmbracoObjectTypes.Unknown;
    }

    protected override async Task<IReadOnlyList<OrderedNodeInfo>> GetMergedItemsAsync(string[] folders, SyncMergeOptions options)
    {
        var items = await base.GetMergedItemsAsync(folders, options);
        return [.. items.OrderBy(x => x.Level)];
    }

    protected override Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(WorkspaceGroup parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
        => Task.FromResult(Enumerable.Empty<uSyncAction>());

    protected override async Task<IEnumerable<WorkspaceGroup>> GetChildItemsAsync(WorkspaceGroup? parent)
        => parent is null ? await _automateService.GetAllGroupsAsync() : [];

    protected override Task<IEnumerable<WorkspaceGroup>> GetFoldersAsync(WorkspaceGroup? parent)
        => Task.FromResult(Enumerable.Empty<WorkspaceGroup>());

    protected override Task<WorkspaceGroup?> GetFromServiceAsync(WorkspaceGroup? item)
        => item is null ? Task.FromResult<WorkspaceGroup?>(null) : _automateService.GetGroupAsync(item.Id);

    protected override string GetItemName(WorkspaceGroup item) => item.Name;

    public Task HandleAsync(WorkspaceGroupSavedNotification notification, CancellationToken cancellationToken)
        => HandleAsync(new AutomateSavedNotification<WorkspaceGroup>(notification.WorkspaceGroup, notification.Messages), cancellationToken);

    public Task HandleAsync(WorkspaceGroupDeletedNotification notification, CancellationToken cancellationToken)
        => HandleAsync(new AutomateDeletedNotification<WorkspaceGroup>(notification.WorkspaceGroup, notification.Messages), cancellationToken);
}
