using Umbraco.Automate.Core.Actions;
using Umbraco.Cms.Core;

using uSync.Automate.Actions.Complete.Actions.Models;
using uSync.Automate.Actions.Complete.Processing;
using uSync.Publisher.Models;

namespace uSync.Automate.Actions.Complete.Actions;

/// <summary>
/// Pulls content or media from another server via uSync.Complete's publisher. Runs inline and
/// blocks the workflow step for as long as the pull takes - a large tree can take minutes.
/// </summary>
[Action(
    "uSyncComplete.pull",
    "uSync Complete: Pull",
    Description = "Pulls content or media from another server.",
    Group = "uSync.Complete",
    Icon = "icon-shipping",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncCompletePullAction : ActionBase<uSyncCompletePullSettings, CompleteRunOutput>
{
    private readonly IProcessingRunner _runner;

    public uSyncCompletePullAction(ActionInfrastructure infrastructure, IProcessingRunner runner) : base(infrastructure)
        => _runner = runner;

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<uSyncCompletePullSettings>();
        var options = uSyncCompletePublishOptionsBuilder.Build(settings, PublishMode.Pull);

        var output = await _runner.RunAsync(CompletePipelines.PublisherPipeline, CompletePipelines.StrategyFor(PublishMode.Pull), options, settings.MaxWaitSeconds, cancellationToken);

        return output.Success
            ? Success(output)
            : ActionResult.Failed(new InvalidOperationException(output.ErrorMessage ?? "uSync.Complete pull failed."), StepRunErrorCategory.Unknown);
    }
}
