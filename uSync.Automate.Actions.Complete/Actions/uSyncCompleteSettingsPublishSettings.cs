using Umbraco.Automate.Core.Settings;

namespace uSync.Automate.Actions.Complete.Actions;

/// <summary>
/// Shared settings for the settings push and pull actions - a single item, identified by its
/// Umbraco UDI entity type and key, for anything other than content/media (document types, data
/// types, templates, dictionary items, member types, or a third-party item type). Unlike the
/// content/media push/pull action, entity type here is free text rather than a dropdown, since
/// there's no fixed, enumerable list of "everything else".
/// </summary>
public abstract class uSyncCompleteSettingsPublishSettingsBase
{
    [Field(Label = "Server", Description = "Alias of the configured uSync.Complete publisher server.", SupportsBindings = true)]
    public string Server { get; set; } = string.Empty;

    [Field(Label = "Entity Type", Description = "Umbraco UDI entity type of the item to sync, e.g. document-type, data-type, template, member.", SortOrder = 1, SupportsBindings = true)]
    public string EntityType { get; set; } = string.Empty;

    [Field(Label = "Key", Description = "Guid key of the item to sync.", SortOrder = 2, SupportsBindings = true)]
    public string Key { get; set; } = string.Empty;

    [Field(Label = "Include Children", Description = "Include the item's descendants, where the entity type supports a tree (e.g. content types).", SortOrder = 3)]
    public bool IncludeChildren { get; set; } = true;

    [Field(Label = "Include Media", Description = "Include media referenced by the item.", SortOrder = 4)]
    public bool IncludeMedia { get; set; }

    [Field(Label = "Include Ancestors", Description = "Include the item's ancestors (so it has somewhere to go on the target).", SortOrder = 5)]
    public bool IncludeAncestors { get; set; } = true;

    [Field(Label = "Include Dependencies", Description = "Include other items this one depends on.", SortOrder = 6, Group = "Advanced")]
    public bool IncludeDependencies { get; set; } = true;

    [Field(Label = "Create Restore Point First", Description = "Take a restore point on the target before applying changes.", SortOrder = 7, Group = "Advanced")]
    public bool CreateRestorePoint { get; set; }

    [Field(Label = "Max Wait Seconds", Description = "How long to wait for another uSync operation to finish before giving up (0 = don't wait).", SortOrder = 8, Group = "Advanced")]
    public int MaxWaitSeconds { get; set; } = 30;
}

public sealed class uSyncCompleteSettingsPushSettings : uSyncCompleteSettingsPublishSettingsBase
{
}

public sealed class uSyncCompleteSettingsPullSettings : uSyncCompleteSettingsPublishSettingsBase
{
}
