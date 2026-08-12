using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core;
using uSync.Automate.Actions.Triggers.Mapping;
using uSync.BackOffice;

namespace uSync.Automate.Actions.Triggers.Bulk;

[Trigger(
    "uSync.exportCompleted",
    "uSync: Export Completed",
    Description = "Fires when a uSync export finishes.",
    Group = "uSync",
    Icon = "icon-arrow-up",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncExportCompletedTrigger
    : NotificationTriggerBase<uSyncBulkTriggerSettings, uSyncBulkTriggerOutput, uSyncExportCompletedNotification>
{
    public uSyncExportCompletedTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(uSyncExportCompletedNotification notification)
    {
        yield return new TriggerEvent<uSyncBulkTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = TriggerInitiatorType.System,
            Output = uSyncActionMapper.ToBulkTriggerOutput("export", notification.Group, notification.Actions.ToList(), uSyncActionMapper.DefaultTriggerChangeCap),
        };
    }

    protected override bool CanHandle(uSyncBulkTriggerOutput output, uSyncBulkTriggerSettings? settings)
        => uSyncBulkTriggerFilter.Matches(output, settings);
}
