using Umbraco.Automate.Core.Settings;

namespace uSync.Automate.Actions.Actions;

public sealed class uSyncExportItemSettings
{
    [Field(
        Label = "Entity Type",
        Description = "The Umbraco entity type of the item to export.",
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = """[{ "alias": "items", "value": ["document", "media", "document-type", "media-type", "member-type", "data-type", "dictionary-item", "template", "language", "relation-type"] }]""")]
    public string EntityType { get; set; } = "document";

    [Field(Label = "Item Key", Description = "The Guid key of the item to export.", SortOrder = 1, SupportsBindings = true)]
    public string ItemKey { get; set; } = string.Empty;

    [Field(Label = "Set", Description = "uSync handler set to use (blank = the default set).", SortOrder = 2, Group = "Advanced")]
    public string? Set { get; set; }

    [Field(Label = "Folders", Description = "Comma-separated folders to export into (blank = uSync's configured folders).", SortOrder = 3, Group = "Advanced")]
    public string? Folders { get; set; }
}
