using Umbraco.Automate.Core.Settings;

namespace uSync.Automate.Actions.Actions;

public sealed class uSyncImportSettings
{
    [Field(Label = "Group", Description = "Handler group to import (blank = the site's default enabled groups).", SupportsBindings = true)]
    public string? Group { get; set; }

    [Field(Label = "Set", Description = "uSync handler set to use (blank = the default set).", SortOrder = 1, Group = "Advanced")]
    public string? Set { get; set; }

    [Field(Label = "Handler Aliases", Description = "Comma-separated handler aliases to restrict the import to (blank = every handler in the group/set).", SortOrder = 2, Group = "Advanced")]
    public string? HandlerAliases { get; set; }

    [Field(Label = "Force", Description = "Import every item even if uSync detects no change.", SortOrder = 3, Group = "Advanced")]
    public bool Force { get; set; }

    [Field(Label = "Folders", Description = "Comma-separated folders to import from (blank = uSync's configured folders).", SortOrder = 4, Group = "Advanced")]
    public string? Folders { get; set; }

    [Field(Label = "Max Change Details", Description = "Maximum number of per-item changes to include in the output.", SortOrder = 5, Group = "Advanced")]
    public int MaxChangeDetails { get; set; } = 200;

    [Field(Label = "Max Wait Seconds", Description = "How long to wait for another uSync operation to finish before failing (0 = fail immediately).", SortOrder = 6, Group = "Advanced")]
    public int MaxWaitSeconds { get; set; }
}
