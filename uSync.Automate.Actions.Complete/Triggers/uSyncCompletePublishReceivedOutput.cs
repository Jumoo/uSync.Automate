namespace uSync.Automate.Actions.Complete.Triggers;

public sealed class uSyncCompletePublishReceivedOutput
{
    public string? Server { get; init; }
    public string Mode { get; init; } = string.Empty;
    public DateTime CompletedUtc { get; init; }
}
