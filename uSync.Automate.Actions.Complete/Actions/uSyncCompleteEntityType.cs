namespace uSync.Automate.Actions.Complete.Actions;

/// <summary>The two entity types uSync.Complete's publisher can push/pull.</summary>
public enum uSyncCompleteEntityType
{
    Document,
    Media,
}

/// <summary>
/// Dropdown editor config for <see cref="uSyncCompleteEntityType"/>, following the same
/// string-backed-dropdown pattern as <c>uSyncOriginBehaviour</c> in <c>uSync.Automate.Actions</c>.
/// </summary>
public static class uSyncCompleteEntityTypeEditor
{
    public const string EditorUiAlias = "Umb.PropertyEditorUi.Dropdown";

    public const string EditorConfig = """
        [{ "alias": "items", "value": [
            { "name": "Content", "value": "Document" },
            { "name": "Media", "value": "Media" }
        ] }]
        """;

    public static uSyncCompleteEntityType Parse(string? value)
        => Enum.TryParse<uSyncCompleteEntityType>(value, ignoreCase: true, out var parsed) ? parsed : uSyncCompleteEntityType.Document;
}
