using Microsoft.Extensions.Logging;
using uSync.Automate.Actions.Actions.Models;
using uSync.Automate.Actions.Services;
using uSync.Automate.Actions.Triggers.Mapping;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;
using uSyncClass = uSync.BackOffice.uSync;

namespace uSync.Automate.Actions.Actions;

/// <summary>
/// Replicates the start -> per-handler-loop -> post -> finish sequence that
/// <c>uSyncManagementService.PerformActionInternalAsync</c> performs for the management API, but
/// callable directly and synchronously from an Automate action - no HTTP request, no SignalR
/// hub, no background-operation cache. <see cref="ISyncActionService"/> only exposes one handler
/// at a time; every consumer of it (the management API, and now this) has to own this loop.
///
/// Deliberately does not accept a <see cref="CancellationToken"/> mid-handler: none of the
/// underlying uSync APIs accept one, so cancellation can only be observed *between* handlers.
/// </summary>
public sealed class uSyncRunner
{
    private readonly ISyncActionService _actionService;
    private readonly ISyncEventService _eventService;
    private readonly ISyncConfigService _configService;
    private readonly IuSyncOperationGate _gate;
    private readonly ILogger<uSyncRunner> _logger;

    public uSyncRunner(
        ISyncActionService actionService,
        ISyncEventService eventService,
        ISyncConfigService configService,
        IuSyncOperationGate gate,
        ILogger<uSyncRunner> logger)
    {
        _actionService = actionService;
        _eventService = eventService;
        _configService = configService;
        _gate = gate;
        _logger = logger;
    }

    /// <summary>True while a uSync import is in progress - guard export actions against the import-completed-triggers-export cascade.</summary>
    public bool IsPaused => _eventService.IsPaused;

    /// <summary>
    /// Holds uSync's own import pause for the lifetime of the returned lease. <c>ImportSingleItemAsync</c>
    /// (used by <c>uSync.importItem</c>) does not pause itself, so without this, uSync's own
    /// save-triggered export would immediately re-export the item that was just imported.
    /// </summary>
    public IDisposable PauseImports() => _eventService.ImportPause(true);

    public async Task<uSyncRunOutput> RunAsync(uSyncRunRequest request, CancellationToken cancellationToken)
    {
        var operation = request.Action.ToString().ToLowerInvariant();

        using var lease = await _gate.TryAcquireAsync(TimeSpan.FromSeconds(Math.Max(0, request.MaxWaitSeconds)), cancellationToken);
        if (lease is null)
        {
            _logger.LogWarning("uSync {Operation}: another uSync operation is already running - skipped", operation);
            return uSyncActionMapper.ToRunOutput(operation, [], request.MaxChangeDetails, success: false,
                errorMessage: "Another uSync operation is already running.");
        }

        try
        {
            var group = string.IsNullOrWhiteSpace(request.Group) ? uSyncClass.EverythingGroupName : request.Group;
            var set = string.IsNullOrWhiteSpace(request.Set) ? _configService.Settings.DefaultSet : request.Set;
            var folders = request.Folders is { Length: > 0 } ? request.Folders : _configService.GetFolders();

            var handlers = _actionService
                .GetActionHandlers(request.Action, new uSyncOptions { Group = group, Set = set, Force = request.Force, Clean = request.Clean })
                .ToList();

            if (request.HandlerAliases is { Length: > 0 })
            {
                handlers = [.. handlers.Where(h => request.HandlerAliases.Contains(h.Alias, StringComparer.OrdinalIgnoreCase))];
            }

            await _actionService.StartProcessAsync(new SyncStartActionRequest
            {
                HandlerAction = request.Action,
                Username = request.Username,
                Clean = request.Clean,
            });

            var handlerMethod = GetHandlerMethod(request.Action);
            var actions = new List<uSyncAction>();

            foreach (var handler in handlers)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("uSync {Operation}: cancelled after {Count}/{Total} handlers", operation, actions.Count, handlers.Count);
                    break;
                }

                var handlerOptions = new SyncActionOptions
                {
                    Folders = folders,
                    Set = set,
                    Group = group,
                    Force = request.Force,
                    Handler = handler.Alias,
                };

                var result = await handlerMethod(handlerOptions, null);
                actions = actions.Merge(result.Actions);

                _logger.LogInformation("uSync {Operation}: {Handler} - {Count} items", operation, handler.Alias, result.Actions.Count);
            }

            var finalOptions = new SyncActionOptions { Folders = folders, Set = set, Group = group, Force = request.Force };
            var finalRequest = new SyncFinalActionRequest
            {
                RequestId = Guid.NewGuid(),
                HandlerAction = request.Action,
                ActionOptions = finalOptions,
                Actions = actions,
                Username = request.Username,
            };

            if (request.Action == HandlerActions.Import)
            {
                finalRequest.Actions = actions;
                var postResult = await _actionService.ImportPostAsync(finalRequest);
                actions = actions.Merge(postResult.Actions);
            }

            finalRequest.Actions = actions;
            var finishResult = await _actionService.FinishProcessAsync(finalRequest);
            actions = actions.Merge(finishResult.Actions);

            var visible = actions.Where(a => a.Change != ChangeType.Hidden).ToList();

            return uSyncActionMapper.ToRunOutput(operation, visible, request.MaxChangeDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "uSync {Operation} failed", operation);
            return uSyncActionMapper.ToRunOutput(operation, [], request.MaxChangeDetails, success: false, errorMessage: ex.Message);
        }
    }

    private Func<SyncActionOptions, uSyncCallbacks?, Task<SyncActionResult>> GetHandlerMethod(HandlerActions action) => action switch
    {
        HandlerActions.Import => _actionService.ImportHandlerAsync,
        HandlerActions.Export => _actionService.ExportHandlerAsync,
        HandlerActions.Report => _actionService.ReportHandlerAsync,
        _ => throw new InvalidOperationException($"uSyncRunner does not support {action}."),
    };
}
