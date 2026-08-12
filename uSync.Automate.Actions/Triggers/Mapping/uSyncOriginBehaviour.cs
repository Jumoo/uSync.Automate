using Umbraco.Automate.Core.Triggers;

namespace uSync.Automate.Actions.Triggers.Mapping;

/// <summary>
/// Shared dropdown config and parsing for the "when triggered by another automation" setting
/// every uSync trigger exposes, mirroring Automate's own <c>ContentSavedTriggerSettings</c>
/// pattern exactly (a string-backed dropdown, parsed via an explicit
/// <see cref="IAutomationOriginatedEventBehavior"/> implementation on the settings class).
/// Public so a later uSync.Complete integration (publisher push/pull triggers) can reuse the
/// same editor config and parsing rather than duplicating it.
/// </summary>
public static class uSyncOriginBehaviour
{
    public const string EditorUiAlias = "Umb.PropertyEditorUi.Dropdown";

    public const string EditorConfig = """
        [{ "alias": "items", "value": [
            { "name": "Always run", "value": "Run" },
            { "name": "Skip if this would loop", "value": "SkipOnCycle" },
            { "name": "Skip entirely", "value": "SkipAlways" }
        ] }]
        """;

    public static AutomationOriginatedEventBehavior Parse(string? value, AutomationOriginatedEventBehavior fallback)
        => Enum.TryParse<AutomationOriginatedEventBehavior>(value, ignoreCase: true, out var parsed) ? parsed : fallback;
}
