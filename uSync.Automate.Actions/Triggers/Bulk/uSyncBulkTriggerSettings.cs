using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using uSync.Automate.Actions.Triggers.Mapping;

namespace uSync.Automate.Actions.Triggers.Bulk;

public sealed class uSyncBulkTriggerSettings : IAutomationOriginatedEventBehavior
{
    [Field(Label = "Groups", Description = "Only fire for these uSync handler groups (comma-separated). Leave blank to match any.")]
    public string? Groups { get; set; }

    [Field(Label = "Only when there are changes", Description = "Skip when the run made no changes.", SortOrder = 1)]
    public bool OnlyWhenChanges { get; set; }

    [Field(Label = "Only when there are errors", Description = "Skip unless the run reported at least one error.", SortOrder = 2)]
    public bool OnlyWhenErrors { get; set; }

    [Field(
        Label = "When triggered by another automation",
        Description = "How to handle a uSync run performed by another automation.",
        EditorUiAlias = uSyncOriginBehaviour.EditorUiAlias,
        EditorConfig = uSyncOriginBehaviour.EditorConfig,
        Group = "Advanced")]
    public string OnAutomationOriginatedEvent { get; set; } = nameof(AutomationOriginatedEventBehavior.SkipOnCycle);

    AutomationOriginatedEventBehavior IAutomationOriginatedEventBehavior.OnAutomationOriginated
        => uSyncOriginBehaviour.Parse(OnAutomationOriginatedEvent, AutomationOriginatedEventBehavior.SkipOnCycle);
}
