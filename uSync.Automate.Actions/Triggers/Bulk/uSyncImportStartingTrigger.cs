using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core;
using uSync.Automate.Actions.Services;
using uSync.BackOffice;

namespace uSync.Automate.Actions.Triggers.Bulk;

/// <summary>
/// Fires when a uSync import begins. This cannot stop the import - <c>CancelableuSyncBulkNotification</c>
/// is only cancelable in-process at the point uSync raises it, and outbox dispatch to an
/// automation happens asynchronously well after that point.
/// </summary>
[Trigger(
    "uSync.importStarting",
    "uSync: Import Starting",
    Description = "Fires when a uSync import begins. This cannot stop the import.",
    Group = "uSync",
    Icon = "icon-arrow-down",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncImportStartingTrigger
    : NotificationTriggerBase<uSyncStartingTriggerSettings, uSyncStartingTriggerOutput, uSyncImportStartingNotification>
{
    private readonly IuSyncItemTriggerBudget _itemTriggerBudget;

    public uSyncImportStartingTrigger(TriggerInfrastructure infrastructure, IuSyncItemTriggerBudget itemTriggerBudget)
        : base(infrastructure)
    {
        _itemTriggerBudget = itemTriggerBudget;
    }

    public override IEnumerable<TriggerEvent> MapEvent(uSyncImportStartingNotification notification)
    {
        // Resets the per-run cap that uSync.itemImported enforces, so a fresh import gets a
        // fresh budget rather than inheriting whatever was left over from the previous one.
        _itemTriggerBudget.Reset();

        yield return new TriggerEvent<uSyncStartingTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = TriggerInitiatorType.System,
            Output = new uSyncStartingTriggerOutput { Operation = "import", StartedUtc = DateTime.UtcNow },
        };
    }
}
