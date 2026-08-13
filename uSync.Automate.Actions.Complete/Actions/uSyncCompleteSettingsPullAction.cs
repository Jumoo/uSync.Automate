using Umbraco.Automate.Core.Actions;
using Umbraco.Cms.Core;

using uSync.Automate.Actions.Complete.Actions.Models;
using uSync.Automate.Actions.Complete.Processing;
using uSync.Publisher.Models;

namespace uSync.Automate.Actions.Complete.Actions;

/// <summary>
/// Pulls a single item of any entity type (document type, data type, template, member, or a
/// third-party item) from another server via uSync.Complete's publisher, identified by its UDI
/// entity type and key. Runs inline and blocks the workflow step for as long as the pull takes.
/// </summary>
[Action(
    "uSyncComplete.pullSettings",
    "uSync Complete: Pull Settings",
    Description = "Pulls a settings item (document type, data type, template, etc.) from another server.",
    Group = "uSync.Complete",
    Icon = "icon-shipping",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncCompleteSettingsPullAction : ActionBase<uSyncCompleteSettingsPullSettings, CompleteRunOutput>
{
    private readonly IProcessingRunner _runner;

    public uSyncCompleteSettingsPullAction(ActionInfrastructure infrastructure, IProcessingRunner runner) : base(infrastructure)
        => _runner = runner;

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<uSyncCompleteSettingsPullSettings>();
        var options = uSyncCompleteSettingsPublishOptionsBuilder.Build(settings, PublishMode.Pull);

        var output = await _runner.RunAsync(CompletePipelines.PublisherPipeline, CompletePipelines.StrategyFor(PublishMode.Pull), options, settings.MaxWaitSeconds, cancellationToken);

        return output.Success
            ? Success(output)
            : ActionResult.Failed(new InvalidOperationException(output.ErrorMessage ?? "uSync.Complete settings pull failed."), StepRunErrorCategory.Unknown);
    }
}
