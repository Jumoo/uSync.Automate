namespace uSync.Automate.Actions.Actions.Models;

/// <summary>
/// JSON-serialisable projection of a uSync <c>uSyncAction</c> - drops the untyped <c>Item</c>
/// (marked <c>[JsonIgnore]</c> on the source struct anyway) and flattens <c>Exception</c> to a
/// string, since binding expressions and the WorkflowCore-persisted output need plain data.
/// <c>Change</c> is always the enum's name, never its numeric value: <c>ChangeType</c> has
/// negative members and gaps, so a raw int is meaningless to compare in a binding expression.
/// Public and stable - a later publisher (uSync.Complete) integration reuses this shape rather
/// than duplicating it, since publisher events also carry <c>IEnumerable&lt;uSyncAction&gt;</c>.
/// </summary>
public sealed class uSyncActionOutput
{
    public string? HandlerAlias { get; init; }
    public string ItemType { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Path { get; init; }
    public Guid Key { get; init; }
    public string Change { get; init; } = "NoChange";
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? DetailMessage { get; init; }
    public string? FileName { get; init; }
    public string? ExceptionMessage { get; init; }
    public int DetailCount { get; init; }
}
