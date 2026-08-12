using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core;
using uSync.Automate.Actions.Configuration;
using uSync.Automate.Actions.Services;
using uSync.Automate.Actions.Triggers.Mapping;
using uSync.BackOffice;

namespace uSync.Automate.Actions.Triggers.Items;

/// <summary>
/// Fires per item on uSync import. Default-off (<c>uSync:Automate:Actions:ItemTriggers:Enabled</c>)
/// and capped per run (<c>MaxEventsPerOperation</c>) - a full-site import can raise thousands of
/// these, and <c>ITriggerSubscriptionRegistry</c> only saves the cost when nobody subscribes.
/// The recommended pattern for bulk follow-up work is <c>uSync.importCompleted</c> +
/// <c>umbracoAutomate.forEach</c> over its capped <c>Changes</c> array instead of this trigger.
/// </summary>
[Trigger(
    "uSync.itemImported",
    "uSync: Item Imported",
    Description = "Fires for each item uSync imports. Off by default and capped per run - see uSync:Automate:Actions:ItemTriggers in configuration.",
    Group = "uSync",
    Icon = "icon-arrow-down",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncItemImportedTrigger
    : NotificationTriggerBase<uSyncItemTriggerSettings, uSyncItemTriggerOutput, uSyncImportedItemNotification>
{
    private readonly IOptionsMonitor<uSyncAutomateActionsOptions> _options;
    private readonly IuSyncItemTriggerBudget _budget;
    private readonly ILogger<uSyncItemImportedTrigger> _logger;

    public uSyncItemImportedTrigger(
        TriggerInfrastructure infrastructure,
        IOptionsMonitor<uSyncAutomateActionsOptions> options,
        IuSyncItemTriggerBudget budget,
        ILogger<uSyncItemImportedTrigger> logger)
        : base(infrastructure)
    {
        _options = options;
        _budget = budget;
        _logger = logger;
    }

    public override IEnumerable<TriggerEvent> MapEvent(uSyncImportedItemNotification notification)
    {
        var itemOptions = _options.CurrentValue.ItemTriggers;
        var output = uSyncItemEventMapper.TryMap("import", notification.Item, notification.Change, itemOptions);
        if (output is null) yield break;

        if (!_budget.TryConsume(itemOptions.MaxEventsPerOperation))
        {
            _logger.LogDebug("uSync.itemImported: per-run event cap ({Max}) reached - further item events suppressed this run", itemOptions.MaxEventsPerOperation);
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
