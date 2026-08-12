using Umbraco.Automate.Core.Settings;

namespace uSync.Automate.Actions.Actions;

public sealed class uSyncReportSettings
{
    [Field(Label = "Group", Description = "Handler group to report on (blank = the site's default enabled groups).", SupportsBindings = true)]
    public string? Group { get; set; }

    [Field(Label = "Set", Description = "uSync handler set to use (blank = the default set).", SortOrder = 1, Group = "Advanced")]
    public string? Set { get; set; }

    [Field(Label = "Handler Aliases", Description = "Comma-separated handler aliases to restrict the report to (blank = every handler in the group/set).", SortOrder = 2, Group = "Advanced")]
    public string? HandlerAliases { get; set; }

    [Field(Label = "Folders", Description = "Comma-separated folders to compare against (blank = uSync's configured folders).", SortOrder = 3, Group = "Advanced")]
    public string? Folders { get; set; }

    [Field(Label = "Max Change Details", Description = "Maximum number of per-item changes to include in the output.", SortOrder = 4, Group = "Advanced")]
    public int MaxChangeDetails { get; set; } = 200;
}
