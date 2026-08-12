using Umbraco.Automate.Core.Actions;
using Umbraco.Cms.Core;
using uSync.Automate.Actions.Actions.Models;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.Automate.Actions.Actions;

/// <summary>
/// Compares the current site against the uSync folder without changing anything - the
/// get-changes/condition-input action. <see cref="uSyncRunOutput.HasChanges"/>/<see cref="uSyncRunOutput.Changes"/>
/// are what a following <c>umbracoAutomate.if</c> or <c>umbracoAutomate.forEach</c> step reads.
/// </summary>
[Action(
    "uSync.report",
    "uSync: Report",
    Description = "Compares the site against the uSync folder and reports what would change, without changing anything.",
    Group = "uSync",
    Icon = "icon-search",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncReportAction : ActionBase<uSyncReportSettings, uSyncRunOutput>
{
    private readonly uSyncRunner _runner;

    public uSyncReportAction(ActionInfrastructure infrastructure, uSyncRunner runner)
        : base(infrastructure)
    {
        _runner = runner;
    }

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<uSyncReportSettings>();

        var request = new uSyncRunRequest
        {
            Action = HandlerActions.Report,
            Group = settings.Group,
            Set = settings.Set,
            HandlerAliases = CsvParsing.ParseOrNull(settings.HandlerAliases),
            Folders = CsvParsing.ParseOrNull(settings.Folders),
            MaxChangeDetails = settings.MaxChangeDetails,
            Username = context.ExecutionContext?.FormatPerformingDetails(),
        };

        var output = await _runner.RunAsync(request, cancellationToken);

        return output.Success
            ? Success(output)
            : ActionResult.Failed(new InvalidOperationException(output.ErrorMessage ?? "uSync report failed."), StepRunErrorCategory.Unknown);
    }
}
