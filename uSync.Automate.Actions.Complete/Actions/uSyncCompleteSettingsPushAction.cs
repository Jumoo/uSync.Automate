using Umbraco.Automate.Core.Actions;
using Umbraco.Cms.Core;

using uSync.Automate.Actions.Complete.Actions.Models;
using uSync.Automate.Actions.Complete.Processing;
using uSync.Publisher.Models;

namespace uSync.Automate.Actions.Complete.Actions;

/// <summary>
/// Pushes a single item of any entity type (document type, data type, template, member, or a
/// third-party item) to another server via uSync.Complete's publisher, identified by its UDI
/// entity type and key. Runs inline and blocks the workflow step for as long as the push takes.
/// </summary>
[Action(
    "uSyncComplete.pushSettings",
    "uSync Complete: Push Settings",
    Description = "Pushes a settings item (document type, data type, template, etc.) to another server.",
    Group = "uSync.Complete",
    Icon = "icon-shipping",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncCompleteSettingsPushAction : ActionBase<uSyncCompleteSettingsPushSettings, CompleteRunOutput>
{
    private readonly IProcessingRunner _runner;

    public uSyncCompleteSettingsPushAction(ActionInfrastructure infrastructure, IProcessingRunner runner) : base(infrastructure)
        => _runner = runner;

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<uSyncCompleteSettingsPushSettings>();
        var options = uSyncCompleteSettingsPublishOptionsBuilder.Build(settings, PublishMode.Push);

        var output = await _runner.RunAsync(CompletePipelines.PublisherPipeline, CompletePipelines.StrategyFor(PublishMode.Push), options, settings.MaxWaitSeconds, cancellationToken);

        return output.Success
            ? Success(output)
            : ActionResult.Failed(new InvalidOperationException(output.ErrorMessage ?? "uSync.Complete settings push failed."), StepRunErrorCategory.Unknown);
    }
}
