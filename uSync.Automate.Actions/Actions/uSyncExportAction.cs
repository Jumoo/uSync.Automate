using Umbraco.Automate.Core.Actions;
using Umbraco.Cms.Core;
using uSync.Automate.Actions.Actions.Models;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.Automate.Actions.Actions;

/// <summary>
/// Exports the site (or part of it) to the uSync folder. Does not mutate the CMS, so not an
/// <see cref="ICmsAction"/>.
///
/// L1 loop-protection guard: skips while a uSync import is in progress
/// (<see cref="uSyncRunner.IsPaused"/>). Without this, a uSync import that saves thousands of
/// content items can fire Automate content triggers, which can run an automation that calls this
/// action, which would immediately re-export the very import that's still running.
/// </summary>
[Action(
    "uSync.export",
    "uSync: Export",
    Description = "Exports the site to the uSync folder.",
    Group = "uSync",
    Icon = "icon-arrow-up",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncExportAction : ActionBase<uSyncExportSettings, uSyncRunOutput>
{
    private readonly uSyncRunner _runner;

    public uSyncExportAction(ActionInfrastructure infrastructure, uSyncRunner runner)
        : base(infrastructure)
    {
        _runner = runner;
    }

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        if (_runner.IsPaused)
            return ActionResult.Skipped("A uSync import is currently running - export skipped to avoid a loop.");

        var settings = context.GetSettings<uSyncExportSettings>();

        var request = new uSyncRunRequest
        {
            Action = HandlerActions.Export,
            Group = settings.Group,
            Set = settings.Set,
            HandlerAliases = CsvParsing.ParseOrNull(settings.HandlerAliases),
            Folders = CsvParsing.ParseOrNull(settings.Folders),
            Clean = settings.CleanFolder,
            MaxChangeDetails = settings.MaxChangeDetails,
            Username = context.ExecutionContext?.FormatPerformingDetails(),
        };

        var output = await _runner.RunAsync(request, cancellationToken);

        return output.Success
            ? Success(output)
            : ActionResult.Failed(new InvalidOperationException(output.ErrorMessage ?? "uSync export failed."), StepRunErrorCategory.Unknown);
    }
}
