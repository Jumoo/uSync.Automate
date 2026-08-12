namespace uSync.Automate.Actions.Actions.Models;

/// <summary>Output of a single-item action (<c>uSync.exportItem</c> / <c>uSync.importItem</c>).</summary>
public sealed class uSyncItemActionOutput
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public uSyncActionOutput? Action { get; init; }
}
