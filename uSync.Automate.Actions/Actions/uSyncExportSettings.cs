using Umbraco.Automate.Core.Settings;

namespace uSync.Automate.Actions.Actions;

public sealed class uSyncExportSettings
{
    [Field(Label = "Group", Description = "Handler group to export (blank = the site's default enabled groups).", SupportsBindings = true)]
    public string? Group { get; set; }

    [Field(Label = "Set", Description = "uSync handler set to use (blank = the default set).", SortOrder = 1, Group = "Advanced")]
    public string? Set { get; set; }

    [Field(Label = "Handler Aliases", Description = "Comma-separated handler aliases to restrict the export to (blank = every handler in the group/set).", SortOrder = 2, Group = "Advanced")]
    public string? HandlerAliases { get; set; }

    [Field(Label = "Clean Folder", Description = "Delete existing files in the target folder(s) before exporting.", SortOrder = 3, Group = "Advanced")]
    public bool CleanFolder { get; set; }

    [Field(Label = "Folders", Description = "Comma-separated folders to export into (blank = uSync's configured folders).", SortOrder = 4, Group = "Advanced")]
    public string? Folders { get; set; }

    [Field(Label = "Max Change Details", Description = "Maximum number of per-item changes to include in the output.", SortOrder = 5, Group = "Advanced")]
    public int MaxChangeDetails { get; set; } = 200;
}
