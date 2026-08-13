using Jumoo.Processing.Core.Pipelines;
using Jumoo.Processing.Core.Pipelines.Models;
using Jumoo.Processing.Core.Processing.Interfaces;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;

using uSync.Automate.Actions.Complete.Actions.Models;
using uSync.Automate.Actions.Services;

namespace uSync.Automate.Actions.Complete.Processing;

/// <summary>
/// Ports <c>uSync.Complete.Hangfire</c>'s <c>ProcessingScheduler</c> (create pipeline -&gt; update
/// options -&gt; poll <see cref="IPipelineService.Process"/> until it stops running) with what
/// that version doesn't have: a re-entrancy gate shared with local uSync run actions
/// (<see cref="IuSyncOperationGate"/>), cancellation between steps, and a result that surfaces
/// the pipeline's own error instead of a bare bool.
/// </summary>
/// <remarks>
/// Runs as the super user - <c>CreatePipeline</c> checks push/pull permission against the
/// passed-in user, and a workflow step has no backoffice user context to hand it.
/// </remarks>
public sealed class ProcessingRunner : IProcessingRunner
{
    /// <summary>Matches the Hangfire scheduler's runaway-loop guard.</summary>
    private const int MaxSteps = 1000;

    private readonly IPipelineService _pipelineService;
    private readonly IUserService _userService;
    private readonly IuSyncOperationGate _gate;
    private readonly ILogger<ProcessingRunner> _logger;

    public ProcessingRunner(
        IPipelineService pipelineService,
        IUserService userService,
        IuSyncOperationGate gate,
        ILogger<ProcessingRunner> logger)
    {
        _pipelineService = pipelineService;
        _userService = userService;
        _gate = gate;
        _logger = logger;
    }

    public async Task<CompleteRunOutput> RunAsync(string pipelineAlias, string strategyAlias, IProcessingOptions options, int maxWaitSeconds, CancellationToken cancellationToken)
    {
        using var lease = await _gate.TryAcquireAsync(TimeSpan.FromSeconds(Math.Max(0, maxWaitSeconds)), cancellationToken);
        if (lease is null)
        {
            _logger.LogWarning("uSync.Complete {Strategy}: another uSync operation is already running - skipped", strategyAlias);
            return Failed("Another uSync operation is already running.");
        }

        try
        {
            var user = await _userService.GetAsync(Constants.Security.SuperUserKey);

            var pipeline = await _pipelineService.CreatePipeline(new CreatePipelineOptions
            {
                Alias = pipelineAlias,
                Strategy = strategyAlias,
                IsInBackground = true,
                User = user,
            });

            await _pipelineService.UpdateOptions(pipeline.Id, options, user);

            return await ProgressAsync(pipeline.Id, user, strategyAlias, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "uSync.Complete {Strategy} failed", strategyAlias);
            return Failed(ex.Message);
        }
    }

    private async Task<CompleteRunOutput> ProgressAsync(Guid pipelineId, IUser? user, string strategyAlias, CancellationToken cancellationToken)
    {
        IPipeline pipeline = await _pipelineService.GetPipeline(pipelineId, user)
            ?? throw new InvalidOperationException($"Pipeline {pipelineId} was not found after creation.");

        var steps = 0;
        while (steps < MaxSteps && pipeline.State.Status == PipelineStatus.Running)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("uSync.Complete {Strategy}: cancelled after {Steps} step(s)", strategyAlias, steps);
                break;
            }

            steps++;
            pipeline = await _pipelineService.Process(pipelineId, user, clientId: string.Empty, isBackgroundRequest: true);
        }

        var completed = pipeline.State.Status == PipelineStatus.Completed;
        return new CompleteRunOutput
        {
            Success = completed,
            Status = pipeline.State.Status.ToString(),
            Message = completed ? "Completed" : pipeline.Results?.Error?.Message,
            ErrorMessage = completed ? null : (pipeline.Results?.Error?.Message ?? $"Pipeline ended in status {pipeline.State.Status}."),
            PipelineId = pipelineId,
            StepCount = steps,
            CompletedUtc = DateTime.UtcNow,
        };
    }

    private static CompleteRunOutput Failed(string message) => new()
    {
        Success = false,
        Status = "Failed",
        ErrorMessage = message,
        CompletedUtc = DateTime.UtcNow,
    };
}
