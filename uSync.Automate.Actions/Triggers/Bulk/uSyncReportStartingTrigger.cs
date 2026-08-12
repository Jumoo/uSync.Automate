using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core;
using uSync.BackOffice;

namespace uSync.Automate.Actions.Triggers.Bulk;

/// <summary>Fires when a uSync report begins. This cannot stop the report - see <see cref="uSyncImportStartingTrigger"/>.</summary>
[Trigger(
    "uSync.reportStarting",
    "uSync: Report Starting",
    Description = "Fires when a uSync report begins. This cannot stop the report.",
    Group = "uSync",
    Icon = "icon-search",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncReportStartingTrigger
    : NotificationTriggerBase<uSyncStartingTriggerSettings, uSyncStartingTriggerOutput, uSyncReportStartingNotification>
{
    public uSyncReportStartingTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(uSyncReportStartingNotification notification)
    {
        yield return new TriggerEvent<uSyncStartingTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = TriggerInitiatorType.System,
            Output = new uSyncStartingTriggerOutput { Operation = "report", StartedUtc = DateTime.UtcNow },
        };
    }
}
