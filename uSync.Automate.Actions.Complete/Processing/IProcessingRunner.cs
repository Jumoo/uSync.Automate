using Jumoo.Processing.Core.Processing.Interfaces;
using uSync.Automate.Actions.Complete.Actions.Models;

namespace uSync.Automate.Actions.Complete.Processing;

/// <summary>
/// Runs a uSync.Complete Jumoo-processing pipeline (publisher push/pull, restore point) to
/// completion, blocking the caller for as long as the pipeline takes.
/// </summary>
public interface IProcessingRunner
{
    Task<CompleteRunOutput> RunAsync(string pipelineAlias, string strategyAlias, IProcessingOptions options, int maxWaitSeconds, CancellationToken cancellationToken);
}
