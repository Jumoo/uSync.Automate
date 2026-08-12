using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core;
using uSync.Automate.Actions.Services;
using uSync.BackOffice;

namespace uSync.Automate.Actions.Triggers.Bulk;

/// <summary>Fires when a uSync export begins. This cannot stop the export - see <see cref="uSyncImportStartingTrigger"/>.</summary>
[Trigger(
    "uSync.exportStarting",
    "uSync: Export Starting",
    Description = "Fires when a uSync export begins. This cannot stop the export.",
    Group = "uSync",
    Icon = "icon-arrow-up",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncExportStartingTrigger
    : NotificationTriggerBase<uSyncStartingTriggerSettings, uSyncStartingTriggerOutput, uSyncExportStartingNotification>
{
    private readonly IuSyncItemTriggerBudget _itemTriggerBudget;

    public uSyncExportStartingTrigger(TriggerInfrastructure infrastructure, IuSyncItemTriggerBudget itemTriggerBudget)
        : base(infrastructure)
    {
        _itemTriggerBudget = itemTriggerBudget;
    }

    public override IEnumerable<TriggerEvent> MapEvent(uSyncExportStartingNotification notification)
    {
        _itemTriggerBudget.Reset();

        yield return new TriggerEvent<uSyncStartingTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = TriggerInitiatorType.System,
            Output = new uSyncStartingTriggerOutput { Operation = "export", StartedUtc = DateTime.UtcNow },
        };
    }
}
