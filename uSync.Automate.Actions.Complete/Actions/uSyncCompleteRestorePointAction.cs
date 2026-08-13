using Umbraco.Automate.Core.Actions;
using Umbraco.Cms.Core;

using uSync.Automate.Actions.Complete.Actions.Models;
using uSync.Automate.Actions.Complete.Processing;
using uSync.Expansions.Core.Restore;
using uSync.Expansions.Core.Restore.Strategies.Models;

namespace uSync.Automate.Actions.Complete.Actions;

/// <summary>Creates a uSync.Complete restore point. Runs inline and blocks the workflow step until it's done.</summary>
[Action(
    "uSyncComplete.createRestorePoint",
    "uSync Complete: Create Restore Point",
    Description = "Takes a restore point of the site.",
    Group = "uSync.Complete",
    Icon = "icon-box",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncCompleteRestorePointAction : ActionBase<uSyncCompleteRestorePointSettings, CompleteRunOutput>
{
    private readonly IProcessingRunner _runner;

    public uSyncCompleteRestorePointAction(ActionInfrastructure infrastructure, IProcessingRunner runner) : base(infrastructure)
        => _runner = runner;

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<uSyncCompleteRestorePointSettings>();

        var options = new RestorePointProcessingOptions
        {
            Source = "Automate",
            Title = string.IsNullOrWhiteSpace(settings.Name) ? $"Automate {DateTime.Now:yyyy-MM-dd HH:mm:ss}" : settings.Name,
            IncludeMedia = settings.IncludeMedia,
            IsBackgroundRequest = true,
        };

        var output = await _runner.RunAsync(uSyncRestorePoints.Pipeline, uSyncRestorePoints.Strategies.Create, options, settings.MaxWaitSeconds, cancellationToken);

        return output.Success
            ? Success(output)
            : ActionResult.Failed(new InvalidOperationException(output.ErrorMessage ?? "uSync.Complete restore point failed."), StepRunErrorCategory.Unknown);
    }
}
