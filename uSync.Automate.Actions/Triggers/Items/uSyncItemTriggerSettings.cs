using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using uSync.Automate.Actions.Triggers.Mapping;

namespace uSync.Automate.Actions.Triggers.Items;

public sealed class uSyncItemTriggerSettings : IAutomationOriginatedEventBehavior
{
    [Field(Label = "Item Types", Description = "Only fire for these uSync XML item types (comma-separated, e.g. Content, DataType). Leave blank to match any.")]
    public string? ItemTypes { get; set; }

    [Field(Label = "Only when changed", Description = "Skip when the item had no change.", SortOrder = 1)]
    public bool OnlyWhenChanges { get; set; } = true;

    [Field(Label = "Only when failed", Description = "Only fire when the item failed to import/export.", SortOrder = 2)]
    public bool OnlyWhenFailed { get; set; }

    [Field(
        Label = "When triggered by another automation",
        Description = "How to handle a uSync run performed by another automation. Item-level uSync events are almost always the tail of a machine-driven cascade, so this defaults to skipping entirely.",
        EditorUiAlias = uSyncOriginBehaviour.EditorUiAlias,
        EditorConfig = uSyncOriginBehaviour.EditorConfig,
        Group = "Advanced")]
    public string OnAutomationOriginatedEvent { get; set; } = nameof(AutomationOriginatedEventBehavior.SkipAlways);

    AutomationOriginatedEventBehavior IAutomationOriginatedEventBehavior.OnAutomationOriginated
        => uSyncOriginBehaviour.Parse(OnAutomationOriginatedEvent, AutomationOriginatedEventBehavior.SkipAlways);
}
