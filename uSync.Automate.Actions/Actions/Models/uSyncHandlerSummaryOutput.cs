namespace uSync.Automate.Actions.Actions.Models;

public sealed class uSyncHandlerSummaryOutput
{
    public string HandlerAlias { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public int ChangeCount { get; init; }
    public int ErrorCount { get; init; }
}
