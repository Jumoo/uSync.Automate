using uSync.Automate.Actions.Actions.Models;

namespace uSync.Automate.Actions.Triggers.Bulk;

public sealed class uSyncBulkTriggerOutput
{
    public string Operation { get; init; } = string.Empty;
    public string? Group { get; init; }
    public int ItemCount { get; init; }
    public int ChangeCount { get; init; }
    public int ErrorCount { get; init; }
    public bool HasChanges { get; init; }
    public bool HasErrors { get; init; }
    public string[] HandlerAliases { get; init; } = [];
    public uSyncHandlerSummaryOutput[] Handlers { get; init; } = [];
    public uSyncActionOutput[] Changes { get; init; } = [];
    public bool ChangesTruncated { get; init; }
    public DateTime CompletedUtc { get; init; }
}
