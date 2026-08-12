using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.Automate.Actions.Actions;

/// <summary>Input to <see cref="uSyncRunner.RunAsync"/> - one bulk uSync operation.</summary>
public sealed class uSyncRunRequest
{
    public required HandlerActions Action { get; init; }
    public string? Group { get; init; }
    public string? Set { get; init; }
    public string[]? HandlerAliases { get; init; }
    public string[]? Folders { get; init; }
    public bool Clean { get; init; }
    public bool Force { get; init; }
    public int MaxChangeDetails { get; init; } = 200;
    public int MaxWaitSeconds { get; init; }
    public string? Username { get; init; }
}
