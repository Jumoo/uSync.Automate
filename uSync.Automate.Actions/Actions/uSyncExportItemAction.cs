using Umbraco.Automate.Core.Actions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;
using uSync.Automate.Actions.Actions.Models;
using uSync.Automate.Actions.Triggers.Mapping;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.Automate.Actions.Actions;

/// <summary>
/// Exports a single item by key - the "when this is published, write it to disk" action.
/// Does not mutate the CMS, so not an <see cref="ICmsAction"/>, but does apply the same L1
/// loop-protection guard as <see cref="uSyncExportAction"/>.
/// </summary>
[Action(
    "uSync.exportItem",
    "uSync: Export Single Item",
    Description = "Exports a single item to the uSync folder.",
    Group = "uSync",
    Icon = "icon-out",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncExportItemAction : ActionBase<uSyncExportItemSettings, uSyncItemActionOutput>
{
    private readonly uSyncRunner _runner;
    private readonly ISyncService _syncService;
    private readonly ISyncConfigService _configService;
    private readonly IUmbracoContextFactory _umbracoContextFactory;

    public uSyncExportItemAction(
        ActionInfrastructure infrastructure,
        uSyncRunner runner,
        ISyncService syncService,
        ISyncConfigService configService,
        IUmbracoContextFactory umbracoContextFactory)
        : base(infrastructure)
    {
        _runner = runner;
        _syncService = syncService;
        _configService = configService;
        _umbracoContextFactory = umbracoContextFactory;
    }

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        if (_runner.IsPaused)
            return ActionResult.Skipped("A uSync import is currently running - export skipped to avoid a loop.");

        var settings = context.GetSettings<uSyncExportItemSettings>();

        if (!Guid.TryParse(settings.ItemKey, out var key))
            return ActionResult.Failed(new ArgumentException($"'{settings.ItemKey}' is not a valid item key."), StepRunErrorCategory.Validation);

        var folders = CsvParsing.ParseOrNull(settings.Folders) ?? _configService.GetFolders();
        var set = string.IsNullOrWhiteSpace(settings.Set) ? _configService.Settings.DefaultSet : settings.Set;
        var udi = Udi.Create(settings.EntityType, key);

        using var umbracoContext = _umbracoContextFactory.EnsureUmbracoContext();

        var actions = (await _syncService.ExportSingleItem(udi, folders, new SyncHandlerOptions(set, HandlerActions.Export))).ToList();
        var action = actions.FirstOrDefault();

        var output = new uSyncItemActionOutput
        {
            Success = action.Success,
            ErrorMessage = action.Success ? null : (action.Message ?? action.Exception?.Message),
            Action = action.Name is null ? null : uSyncActionMapper.ToActionOutput(action),
        };

        return output.Success
            ? Success(output)
            : ActionResult.Failed(new InvalidOperationException(output.ErrorMessage ?? "uSync export item failed."), StepRunErrorCategory.Validation);
    }
}
