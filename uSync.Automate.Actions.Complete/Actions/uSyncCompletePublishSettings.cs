using Umbraco.Automate.Core.Settings;

namespace uSync.Automate.Actions.Complete.Actions;

/// <summary>Shared settings for the publisher push and pull actions.</summary>
public abstract class uSyncCompletePublishSettingsBase
{
    [Field(Label = "Server", Description = "Alias of the configured uSync.Complete publisher server.", SupportsBindings = true)]
    public string Server { get; set; } = string.Empty;

    [Field(Label = "Content Type", Description = "Whether to sync a content item or a media item.",
        EditorUiAlias = uSyncCompleteEntityTypeEditor.EditorUiAlias, EditorConfig = uSyncCompleteEntityTypeEditor.EditorConfig, SortOrder = 1)]
    public string EntityType { get; set; } = nameof(uSyncCompleteEntityType.Document);

    [Field(Label = "Root Key", Description = "Key of the content/media item to sync (blank = the whole tree).", SortOrder = 2, SupportsBindings = true)]
    public string RootKey { get; set; } = string.Empty;

    [Field(Label = "Include Children", Description = "Include the item's descendants.", SortOrder = 3)]
    public bool IncludeChildren { get; set; } = true;

    [Field(Label = "Include Media", Description = "Include media referenced by the item(s).", SortOrder = 4)]
    public bool IncludeMedia { get; set; } = true;

    [Field(Label = "Include Ancestors", Description = "Include the item's ancestors (so it has somewhere to go on the target).", SortOrder = 5)]
    public bool IncludeAncestors { get; set; } = true;

    [Field(Label = "Include Dependencies", Description = "Include other items this one depends on (e.g. pickers).", SortOrder = 6, Group = "Advanced")]
    public bool IncludeDependencies { get; set; }

    [Field(Label = "Create Restore Point First", Description = "Take a restore point on the target before applying changes.", SortOrder = 7, Group = "Advanced")]
    public bool CreateRestorePoint { get; set; }

    [Field(Label = "Max Wait Seconds", Description = "How long to wait for another uSync operation to finish before giving up (0 = don't wait).", SortOrder = 8, Group = "Advanced")]
    public int MaxWaitSeconds { get; set; } = 30;
}

public sealed class uSyncCompletePushSettings : uSyncCompletePublishSettingsBase
{
}

public sealed class uSyncCompletePullSettings : uSyncCompletePublishSettingsBase
{
}
