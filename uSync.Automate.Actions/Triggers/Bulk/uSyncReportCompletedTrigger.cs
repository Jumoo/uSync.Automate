using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core;
using uSync.Automate.Actions.Triggers.Mapping;
using uSync.BackOffice;

namespace uSync.Automate.Actions.Triggers.Bulk;

[Trigger(
    "uSync.reportCompleted",
    "uSync: Report Completed",
    Description = "Fires when a uSync report finishes.",
    Group = "uSync",
    Icon = "icon-search",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncReportCompletedTrigger
    : NotificationTriggerBase<uSyncBulkTriggerSettings, uSyncBulkTriggerOutput, uSyncReportCompletedNotification>
{
    public uSyncReportCompletedTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(uSyncReportCompletedNotification notification)
    {
        yield return new TriggerEvent<uSyncBulkTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = TriggerInitiatorType.System,
            Output = uSyncActionMapper.ToBulkTriggerOutput("report", notification.Group, notification.Actions.ToList(), uSyncActionMapper.DefaultTriggerChangeCap),
        };
    }

    protected override bool CanHandle(uSyncBulkTriggerOutput output, uSyncBulkTriggerSettings? settings)
        => uSyncBulkTriggerFilter.Matches(output, settings);
}
