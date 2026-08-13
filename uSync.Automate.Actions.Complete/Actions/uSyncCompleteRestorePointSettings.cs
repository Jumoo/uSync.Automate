using Umbraco.Automate.Core.Settings;

namespace uSync.Automate.Actions.Complete.Actions;

public sealed class uSyncCompleteRestorePointSettings
{
    [Field(Label = "Name", Description = "Name for the restore point (blank = a timestamp).", SupportsBindings = true)]
    public string? Name { get; set; }

    [Field(Label = "Include Media", Description = "Include media in the restore point.", SortOrder = 1)]
    public bool IncludeMedia { get; set; } = true;

    [Field(Label = "Max Wait Seconds", Description = "How long to wait for another uSync operation to finish before giving up (0 = don't wait).", SortOrder = 2, Group = "Advanced")]
    public int MaxWaitSeconds { get; set; } = 30;
}
