using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using uSync.Automate.Actions.Triggers.Mapping;

namespace uSync.Automate.Actions.Complete.Triggers;

/// <summary>
/// A publish landing here can fire thousands of Umbraco content-saved notifications, which can
/// trigger Automate content automations that call back into a push/pull - see the loop-protection
/// guidance in <c>uSync.Automate.Actions</c>'s readme. Defaults itself to a non-empty instance so
/// Automate's loop-protection check actually runs (it's skipped for empty trigger settings).
/// </summary>
public sealed class uSyncCompletePublishReceivedSettings : IAutomationOriginatedEventBehavior
{
    [Field(Label = "Server", Description = "Only fire for publishes received from this server alias (blank = any).")]
    public string? Server { get; set; }

    [Field(
        Label = "When triggered by another automation",
        Description = "How to handle a publish performed by another automation.",
        EditorUiAlias = uSyncOriginBehaviour.EditorUiAlias,
        EditorConfig = uSyncOriginBehaviour.EditorConfig,
        Group = "Advanced")]
    public string OnAutomationOriginatedEvent { get; set; } = nameof(AutomationOriginatedEventBehavior.SkipOnCycle);

    AutomationOriginatedEventBehavior IAutomationOriginatedEventBehavior.OnAutomationOriginated
        => uSyncOriginBehaviour.Parse(OnAutomationOriginatedEvent, AutomationOriginatedEventBehavior.SkipOnCycle);
}
