using Microsoft.Extensions.Logging;
using Umbraco.Automate.Core.Workspaces;

namespace uSync.Automate.Services;

public sealed partial class SyncAutomateService
{
    public Task<WorkspaceGroup?> GetGroupAsync(Guid key)
        => RunAsync(() => _groups.GetGroupAsync(key), nameof(GetGroupAsync));

    public Task<IEnumerable<WorkspaceGroup>> GetAllGroupsAsync()
        => RunAsync(() => _groups.GetAllGroupsAsync(), nameof(GetAllGroupsAsync));

    public Task<IEnumerable<WorkspaceGroup>> GetChildGroupsAsync(Guid workspaceId, Guid? parentId)
        => RunAsync(() => _groups.GetGroupsByWorkspaceAsync(workspaceId, parentId), nameof(GetChildGroupsAsync));

    /// <summary>
    /// Resolves a group by its synthetic workspace-qualified path ("workspaceAlias/a/b"), walking
    /// the tree by <see cref="WorkspaceGroup.Name"/> at each level. This is the fallback identity
    /// used when a group's key differs across servers (e.g. created by hand on both).
    /// </summary>
    public async Task<WorkspaceGroup?> GetGroupByPathAsync(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return null;

        var workspace = await GetWorkspaceAsync(segments[0]);
        if (workspace is null) return null;

        WorkspaceGroup? current = null;
        Guid? parentId = null;

        for (var i = 1; i < segments.Length; i++)
        {
            var children = await GetChildGroupsAsync(workspace.Id, parentId);
            current = children.FirstOrDefault(g => string.Equals(g.Name, segments[i], StringComparison.OrdinalIgnoreCase));
            if (current is null) return null;
            parentId = current.Id;
        }

        return current;
    }

    /// <summary>
    /// Builds the portable path for a group: "{workspaceAlias}/{ancestorName}/.../{name}".
    /// </summary>
    public async Task<string> GetGroupPathAsync(WorkspaceGroup item)
    {
        var names = new List<string> { item.Name };

        var parentId = item.ParentId;
        while (parentId is not null)
        {
            var parent = await GetGroupAsync(parentId.Value);
            if (parent is null) break;
            names.Insert(0, parent.Name);
            parentId = parent.ParentId;
        }

        var workspaceAlias = await GetWorkspaceAliasAsync(item.WorkspaceId) ?? item.WorkspaceId.ToString();
        return string.Join('/', [workspaceAlias, .. names]);
    }

    /// <summary>Tree depth - root groups (no parent) are level 0.</summary>
    public async Task<int> GetGroupLevelAsync(WorkspaceGroup item)
    {
        var level = 0;
        var parentId = item.ParentId;
        while (parentId is not null)
        {
            var parent = await GetGroupAsync(parentId.Value);
            if (parent is null) break;
            level++;
            parentId = parent.ParentId;
        }

        return level;
    }

    public async Task SaveGroupAsync(WorkspaceGroup item)
    {
        var existing = await GetGroupAsync(item.Id);
        if (existing is null)
        {
            await RunAsync(() => _groups.CreateGroupAsync(item), nameof(SaveGroupAsync));
            return;
        }

        if (existing.WorkspaceId != item.WorkspaceId)
        {
            _logger.LogWarning(
                "AutomateGroup {Key} cannot move between workspaces (existing {ExistingWorkspace}, incoming {IncomingWorkspace}); workspace left unchanged",
                item.Id, existing.WorkspaceId, item.WorkspaceId);
        }

        existing.Name = item.Name;
        existing.ParentId = item.ParentId;

        await RunAsync(() => _groups.UpdateGroupAsync(existing), nameof(SaveGroupAsync));
    }

    public Task<bool> DeleteGroupAsync(WorkspaceGroup item)
        => RunAsync(() => _groups.DeleteGroupAsync(item.Id), nameof(DeleteGroupAsync));
}
