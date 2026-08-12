using Umbraco.Automate.Core.Actions;
using Umbraco.Cms.Core;
using uSync.Automate.Actions.Actions.Models;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.Automate.Actions.Actions;

/// <summary>
/// Imports from the uSync folder into the site. <see cref="ICmsAction"/> so a successful import
/// writes a CMS audit trail entry with the service-account identity.
/// </summary>
[Action(
    "uSync.import",
    "uSync: Import",
    Description = "Imports the uSync folder into the site.",
    Group = "uSync",
    Icon = "icon-arrow-down",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncImportAction : ActionBase<uSyncImportSettings, uSyncRunOutput>, ICmsAction
{
    private readonly uSyncRunner _runner;

    public uSyncImportAction(ActionInfrastructure infrastructure, uSyncRunner runner)
        : base(infrastructure)
    {
        _runner = runner;
    }

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<uSyncImportSettings>();

        var request = new uSyncRunRequest
        {
            Action = HandlerActions.Import,
            Group = settings.Group,
            Set = settings.Set,
            HandlerAliases = CsvParsing.ParseOrNull(settings.HandlerAliases),
            Folders = CsvParsing.ParseOrNull(settings.Folders),
            Force = settings.Force,
            MaxChangeDetails = settings.MaxChangeDetails,
            MaxWaitSeconds = settings.MaxWaitSeconds,
            Username = context.ExecutionContext?.FormatPerformingDetails(),
        };

        var output = await _runner.RunAsync(request, cancellationToken);

        return output.Success
            ? Success(output)
            : ActionResult.Failed(new InvalidOperationException(output.ErrorMessage ?? "uSync import failed."), StepRunErrorCategory.Unknown);
    }
}
