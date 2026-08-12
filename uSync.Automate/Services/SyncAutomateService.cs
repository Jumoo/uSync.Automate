using Microsoft.Extensions.Logging;
using Umbraco.Automate.Core;
using Umbraco.Automate.Core.Automations.Transfer;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Automate.Core.Automations;
using Umbraco.Cms.Core.Services;

namespace uSync.Automate.Services;

/// <summary>
/// The only thing uSync.Automate's handlers and serializers talk to. Wraps Automate's public
/// service layer (repositories are internal to Automate, so the service layer is the only
/// supported route) and absorbs two frictions: uSync's save/delete APIs don't say
/// create-vs-update, and Automate's <c>Version</c> property is internal-set and guarded by
/// optimistic concurrency, so every update must load the existing entity and copy onto it
/// rather than constructing a fresh one.
/// </summary>
public sealed partial class SyncAutomateService
{
    private readonly IAutomationService _automations;
    private readonly IWorkspaceService _workspaces;
    private readonly IWorkspaceGroupService _groups;
    private readonly IConnectionService _connections;
    private readonly ISensitiveSettingsStripper _stripper;
    private readonly IUserGroupService _userGroups;
    private readonly IUserService _users;
    private readonly AutomateReadinessSignal _readiness;
    private readonly ILogger<SyncAutomateService> _logger;

    public SyncAutomateService(
        IAutomationService automations,
        IWorkspaceService workspaces,
        IWorkspaceGroupService groups,
        IConnectionService connections,
        ISensitiveSettingsStripper stripper,
        IUserGroupService userGroups,
        IUserService users,
        AutomateReadinessSignal readiness,
        ILogger<SyncAutomateService> logger)
    {
        _automations = automations;
        _workspaces = workspaces;
        _groups = groups;
        _connections = connections;
        _stripper = stripper;
        _userGroups = userGroups;
        _users = users;
        _readiness = readiness;
        _logger = logger;
    }

    /// <summary>
    /// True once Automate's startup migrations have completed. Reads/writes attempted before
    /// this is true throw <see cref="AutomateNotReadyException"/> deep in Automate's own code -
    /// callers should check this first and fail soft (empty list / friendly message) instead.
    /// </summary>
    public bool IsReady => _readiness.IsReady;

    /// <summary>
    /// Runs an Automate service call, translating Automate's own exceptions into messages that
    /// are safe to surface directly in a <c>SyncAttempt.Fail</c> - Automate's own
    /// <see cref="InvalidOperationException"/> messages are already user-facing.
    /// </summary>
    private async Task<T> RunAsync<T>(Func<Task<T>> action, string operationName)
    {
        try
        {
            return await action();
        }
        catch (AutomateNotReadyException)
        {
            _logger.LogWarning("Automate database is not ready; {Operation} skipped", operationName);
            throw;
        }
        catch (ConcurrencyConflictException ex)
        {
            _logger.LogWarning(ex, "{Operation} hit a concurrency conflict on {EntityType} {EntityId}", operationName, ex.EntityType, ex.EntityId);
            throw new InvalidOperationException($"The item was modified during import; re-run the import. ({ex.Message})", ex);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "{Operation} failed: {Message}", operationName, ex.Message);
            throw;
        }
    }
}
