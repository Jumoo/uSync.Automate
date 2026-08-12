using Umbraco.Automate.Core.Actions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;
using uSync.Automate.Actions.Actions.Models;
using uSync.Automate.Actions.Triggers.Mapping;
using uSync.BackOffice;

namespace uSync.Automate.Actions.Actions;

/// <summary>
/// Imports a single item by key and handler alias. <see cref="ISyncService.ImportSingleItemAsync"/>
/// always imports with <c>SerializerFlags.Force</c> (no opt-out) and, unlike the bulk import path,
/// does not pause uSync's own event handling first - so this action wraps the call in
/// <see cref="uSyncRunner.PauseImports"/> itself (L2 loop protection), or uSync's save-triggered
/// export would immediately re-export the item this action just imported.
/// </summary>
[Action(
    "uSync.importItem",
    "uSync: Import Single Item",
    Description = "Imports a single item from the uSync folder. Always imports, even if uSync detects no change.",
    Group = "uSync",
    Icon = "icon-download",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncImportItemAction : ActionBase<uSyncImportItemSettings, uSyncItemActionOutput>, ICmsAction
{
    private readonly uSyncRunner _runner;
    private readonly ISyncService _syncService;
    private readonly IUmbracoContextFactory _umbracoContextFactory;

    public uSyncImportItemAction(
        ActionInfrastructure infrastructure,
        uSyncRunner runner,
        ISyncService syncService,
        IUmbracoContextFactory umbracoContextFactory)
        : base(infrastructure)
    {
        _runner = runner;
        _syncService = syncService;
        _umbracoContextFactory = umbracoContextFactory;
    }

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<uSyncImportItemSettings>();

        if (!Guid.TryParse(settings.ItemKey, out var key))
            return ActionResult.Failed(new ArgumentException($"'{settings.ItemKey}' is not a valid item key."), StepRunErrorCategory.Validation);

        if (string.IsNullOrWhiteSpace(settings.HandlerAlias))
            return ActionResult.Failed(new ArgumentException("A handler alias is required."), StepRunErrorCategory.Validation);

        using var umbracoContext = _umbracoContextFactory.EnsureUmbracoContext();
        using var pause = _runner.PauseImports();

        var action = await _syncService.ImportSingleItemAsync(key, settings.HandlerAlias);

        var output = new uSyncItemActionOutput
        {
            Success = action.Success,
            ErrorMessage = action.Success ? null : (action.Message ?? action.Exception?.Message),
            Action = action.Name is null ? null : uSyncActionMapper.ToActionOutput(action),
        };

        return output.Success
            ? Success(output)
            : ActionResult.Failed(new InvalidOperationException(output.ErrorMessage ?? "uSync import item failed."), StepRunErrorCategory.Validation);
    }
}
