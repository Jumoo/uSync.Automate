using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core;
using uSync.Automate.Actions.Configuration;
using uSync.Automate.Actions.Services;
using uSync.Automate.Actions.Triggers.Mapping;
using uSync.BackOffice;

namespace uSync.Automate.Actions.Triggers.Items;

/// <summary>Fires per item on uSync export. See <see cref="uSyncItemImportedTrigger"/> for the throttling story.</summary>
[Trigger(
    "uSync.itemExported",
    "uSync: Item Exported",
    Description = "Fires for each item uSync exports. Off by default and capped per run - see uSync:Automate:Actions:ItemTriggers in configuration.",
    Group = "uSync",
    Icon = "icon-arrow-up",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncItemExportedTrigger
    : NotificationTriggerBase<uSyncItemTriggerSettings, uSyncItemTriggerOutput, uSyncExportedItemNotification>
{
    private readonly IOptionsMonitor<uSyncAutomateActionsOptions> _options;
    private readonly IuSyncItemTriggerBudget _budget;
    private readonly ILogger<uSyncItemExportedTrigger> _logger;

    public uSyncItemExportedTrigger(
        TriggerInfrastructure infrastructure,
        IOptionsMonitor<uSyncAutomateActionsOptions> options,
        IuSyncItemTriggerBudget budget,
        ILogger<uSyncItemExportedTrigger> logger)
        : base(infrastructure)
    {
        _options = options;
        _budget = budget;
        _logger = logger;
    }

    public override IEnumerable<TriggerEvent> MapEvent(uSyncExportedItemNotification notification)
    {
        var itemOptions = _options.CurrentValue.ItemTriggers;
        var output = uSyncItemEventMapper.TryMap("export", notification.Item, notification.Change, itemOptions);
        if (output is null) yield break;

        if (!_budget.TryConsume(itemOptions.MaxEventsPerOperation))
        {
            _logger.LogDebug("uSync.itemExported: per-run event cap ({Max}) reached - further item events suppressed this run", itemOptions.MaxEventsPerOperation);
            yield break;
        }

        yield return new TriggerEvent<uSyncItemTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = TriggerInitiatorType.System,
            IdempotencyKey = $"{Alias}:{output.Key}:{output.Change}:{output.Alias}",
            Output = output,
        };
    }

    protected override bool CanHandle(uSyncItemTriggerOutput output, uSyncItemTriggerSettings? settings)
        => uSyncItemTriggerFilter.Matches(output, settings);
}
