namespace uSync.Automate.Actions.Triggers.Items;

public sealed class uSyncItemTriggerOutput
{
    public string Operation { get; init; } = string.Empty;
    public string Change { get; init; } = string.Empty;
    public bool IsChange { get; init; }
    public bool IsFailure { get; init; }
    public string ItemType { get; init; } = string.Empty;
    public Guid Key { get; init; }
    public string Alias { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public int Level { get; init; }
    public bool IsEmptyItem { get; init; }
}
