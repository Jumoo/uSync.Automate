using Umbraco.Automate.Core.Actions;
using Umbraco.Cms.Core;

using uSync.Automate.Actions.Complete.Actions.Models;
using uSync.Automate.Actions.Complete.Processing;
using uSync.Publisher.Models;

namespace uSync.Automate.Actions.Complete.Actions;

/// <summary>
/// Pushes content or media to another server via uSync.Complete's publisher. Runs inline and
/// blocks the workflow step for as long as the push takes - a large tree can take minutes.
/// </summary>
[Action(
    "uSyncComplete.push",
    "uSync Complete: Push",
    Description = "Pushes content or media to another server.",
    Group = "uSync.Complete",
    Icon = "icon-shipping",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncCompletePushAction : ActionBase<uSyncCompletePushSettings, CompleteRunOutput>
{
    private readonly IProcessingRunner _runner;

    public uSyncCompletePushAction(ActionInfrastructure infrastructure, IProcessingRunner runner) : base(infrastructure)
        => _runner = runner;

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<uSyncCompletePushSettings>();
        var options = uSyncCompletePublishOptionsBuilder.Build(settings, PublishMode.Push);

        var output = await _runner.RunAsync(CompletePipelines.PublisherPipeline, CompletePipelines.StrategyFor(PublishMode.Push), options, settings.MaxWaitSeconds, cancellationToken);

        return output.Success
            ? Success(output)
            : ActionResult.Failed(new InvalidOperationException(output.ErrorMessage ?? "uSync.Complete push failed."), StepRunErrorCategory.Unknown);
    }
}
