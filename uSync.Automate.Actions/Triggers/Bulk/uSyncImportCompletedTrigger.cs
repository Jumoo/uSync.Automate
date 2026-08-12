using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core;
using uSync.Automate.Actions.Triggers.Mapping;
using uSync.BackOffice;

namespace uSync.Automate.Actions.Triggers.Bulk;

/// <summary>
/// Fires once a uSync import finishes. <c>IdempotencyKey</c> is deliberately left null:
/// uSync raises this notification exactly once per import, and hand-rolling a content hash
/// would falsely collapse two genuinely separate zero-change imports into one - a missing run is
/// worse than a theoretical duplicate.
/// </summary>
[Trigger(
    "uSync.importCompleted",
    "uSync: Import Completed",
    Description = "Fires when a uSync import finishes.",
    Group = "uSync",
    Icon = "icon-arrow-down",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncImportCompletedTrigger
    : NotificationTriggerBase<uSyncBulkTriggerSettings, uSyncBulkTriggerOutput, uSyncImportCompletedNotification>
{
    public uSyncImportCompletedTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(uSyncImportCompletedNotification notification)
    {
        yield return new TriggerEvent<uSyncBulkTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = TriggerInitiatorType.System,
            Output = uSyncActionMapper.ToBulkTriggerOutput("import", notification.Group, notification.Actions.ToList(), uSyncActionMapper.DefaultTriggerChangeCap),
        };
    }

    protected override bool CanHandle(uSyncBulkTriggerOutput output, uSyncBulkTriggerSettings? settings)
        => uSyncBulkTriggerFilter.Matches(output, settings);
}

