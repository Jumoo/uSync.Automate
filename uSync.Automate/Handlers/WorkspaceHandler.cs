using Microsoft.Extensions.Logging;
using Umbraco.Automate.Core.Notifications;
using Umbraco.Automate.Core.Workspaces;
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
/// Syncs Automate <see cref="Workspace"/>s. Not <see cref="Umbraco.Cms.Core.Models.IEntity"/>, so
/// this derives from <see cref="SyncHandlerRoot{TObject, TContainer}"/> directly rather than the
/// IEntity-constrained base classes uSync's own content-adjacent handlers use.
/// </summary>
[SyncHandler(
    "automateWorkspaceHandler",
    "Workspaces",
    "Automate-Workspaces",
    uSyncAutomatePriorities.Workspaces,
    Icon = "icon-box",
    EntityType = uSyncAutomate.WorkspaceEntityType)]
public class WorkspaceHandler : SyncHandlerRoot<Workspace, Workspace>, ISyncHandler,
    INotificationAsyncHandler<WorkspaceSavedNotification>,
    INotificationAsyncHandler<WorkspaceDeletedNotification>
{
    private readonly SyncAutomateService _automateService;

    public override string Group => uSyncAutomate.GroupName;

    public WorkspaceHandler(
        ILogger<SyncHandlerRoot<Workspace, Workspace>> logger,
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

    protected override Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(Workspace parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
        => Task.FromResult(Enumerable.Empty<uSyncAction>());

    protected override async Task<IEnumerable<Workspace>> GetChildItemsAsync(Workspace? parent)
        => parent is null ? await _automateService.GetAllWorkspacesAsync() : [];

    protected override Task<IEnumerable<Workspace>> GetFoldersAsync(Workspace? parent)
        => Task.FromResult(Enumerable.Empty<Workspace>());

    protected override Task<Workspace?> GetFromServiceAsync(Workspace? item)
        => item is null ? Task.FromResult<Workspace?>(null) : _automateService.GetWorkspaceAsync(item.Id);

    protected override string GetItemName(Workspace item) => item.Name;

    protected override string GetItemPath(Workspace item, bool useGuid, bool isFlat)
        => useGuid ? item.Id.ToString() : item.Alias.ToSafeFileName(shortStringHelper);

    // Automate's own notifications derive from ObjectNotification<T>, not uSync's abstract
    // SavedNotification<T>/DeletedNotification<T> - adapt via the concrete shims so uSync's
    // export-on-save / delete-marker logic in the base class runs unmodified.
    public Task HandleAsync(WorkspaceSavedNotification notification, CancellationToken cancellationToken)
        => HandleAsync(new AutomateSavedNotification<Workspace>(notification.Workspace, notification.Messages), cancellationToken);

    public Task HandleAsync(WorkspaceDeletedNotification notification, CancellationToken cancellationToken)
        => HandleAsync(new AutomateDeletedNotification<Workspace>(notification.Workspace, notification.Messages), cancellationToken);
}
