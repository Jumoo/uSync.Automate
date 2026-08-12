using Umbraco.Automate.Core.Workspaces;

namespace uSync.Automate.Services;

public sealed partial class SyncAutomateService
{
    public Task<Workspace?> GetWorkspaceAsync(Guid key)
        => RunAsync(() => _workspaces.GetWorkspaceAsync(key), nameof(GetWorkspaceAsync));

    public Task<Workspace?> GetWorkspaceAsync(string alias)
        => RunAsync(() => _workspaces.GetWorkspaceByAliasAsync(alias), nameof(GetWorkspaceAsync));

    public Task<IEnumerable<Workspace>> GetAllWorkspacesAsync()
        => RunAsync(() => _workspaces.GetAllWorkspacesAsync(), nameof(GetAllWorkspacesAsync));

    /// <summary>
    /// Create-or-update. Fresh <see cref="Workspace"/> instances have <c>Version = 1</c>, which
    /// would fail Automate's optimistic-concurrency check against any existing target row, so an
    /// update loads the existing entity and copies the portable properties onto it rather than
    /// saving <paramref name="item"/> directly.
    /// </summary>
    public async Task SaveWorkspaceAsync(Workspace item)
    {
        var existing = await RunAsync(() => _workspaces.GetWorkspaceAsync(item.Id), nameof(SaveWorkspaceAsync));
        if (existing is null)
        {
            await RunAsync(() => _workspaces.CreateWorkspaceAsync(item, null), nameof(SaveWorkspaceAsync));
            return;
        }

        existing.Alias = item.Alias;
        existing.Name = item.Name;
        existing.ServiceAccountKey = item.ServiceAccountKey;
        existing.UserGroups = item.UserGroups;
        existing.AllowedConnections = item.AllowedConnections;

        await RunAsync(() => _workspaces.UpdateWorkspaceAsync(existing, null), nameof(SaveWorkspaceAsync));
    }

    public Task<bool> DeleteWorkspaceAsync(Workspace item)
        => RunAsync(() => _workspaces.DeleteWorkspaceAsync(item.Id), nameof(DeleteWorkspaceAsync));

    public async Task<Guid?> ResolveWorkspaceIdAsync(string? alias)
    {
        if (string.IsNullOrWhiteSpace(alias)) return null;
        var workspace = await GetWorkspaceAsync(alias);
        return workspace?.Id;
    }

    public async Task<string?> GetWorkspaceAliasAsync(Guid id)
    {
        var workspace = await GetWorkspaceAsync(id);
        return workspace?.Alias;
    }

    /// <summary>
    /// Resolves a user group by its portable alias, falling back to <paramref name="fallback"/>
    /// (the Guid recorded in the XML) only when a group with that exact key also exists on this
    /// server. Returns null when neither resolves, so the caller can drop the entry and warn
    /// rather than silently wiring the workspace to a nonexistent user group.
    /// </summary>
    public async Task<Guid?> ResolveUserGroupKeyAsync(string? alias, Guid fallback)
    {
        if (!string.IsNullOrWhiteSpace(alias))
        {
            var group = await _userGroups.GetAsync(alias);
            if (group is not null) return group.Key;
        }

        if (fallback == Guid.Empty) return null;

        var byKey = await _userGroups.GetAsync(fallback);
        return byKey is not null ? fallback : null;
    }

    public async Task<string?> GetUserGroupAliasAsync(Guid key)
    {
        var group = await _userGroups.GetAsync(key);
        return group?.Alias;
    }

    /// <summary>Login name of the service account, for the portable text in the &lt;ServiceAccount&gt; element.</summary>
    public async Task<string?> GetServiceAccountUsernameAsync(Guid key)
    {
        var user = await _users.GetAsync(key);
        return user?.Username;
    }

    /// <summary>
    /// Resolves the service account for a workspace by Guid first (works when the same API user
    /// exists on both servers), falling back to the original key with a warning when it doesn't -
    /// publish validation will catch an unresolvable service account and soft-fail in the
    /// automation's second pass rather than blocking the whole import.
    /// </summary>
    public async Task<(Guid key, string? username)> ResolveServiceAccountAsync(Guid key, string? username)
    {
        var byKey = await _users.GetAsync(key);
        if (byKey is not null) return (byKey.Key, byKey.Username);

        return (key, username);
    }
}
