using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using uSync.Automate.Actions.Triggers.Mapping;

namespace uSync.Automate.Actions.Triggers.Bulk;

/// <summary>
/// Settings for the <c>*Starting</c> triggers - lighter than <see cref="uSyncBulkTriggerSettings"/>
/// because <c>CancelableuSyncBulkNotification</c> always carries an empty <c>Actions</c> collection
/// and a null <c>Group</c>, so there's nothing to filter on beyond the origin-behaviour setting.
/// </summary>
public sealed class uSyncStartingTriggerSettings : IAutomationOriginatedEventBehavior
{
    [Field(
        Label = "When triggered by another automation",
        Description = "How to handle a uSync run performed by another automation.",
        EditorUiAlias = uSyncOriginBehaviour.EditorUiAlias,
        EditorConfig = uSyncOriginBehaviour.EditorConfig)]
    public string OnAutomationOriginatedEvent { get; set; } = nameof(AutomationOriginatedEventBehavior.SkipOnCycle);

    AutomationOriginatedEventBehavior IAutomationOriginatedEventBehavior.OnAutomationOriginated
        => uSyncOriginBehaviour.Parse(OnAutomationOriginatedEvent, AutomationOriginatedEventBehavior.SkipOnCycle);
}
