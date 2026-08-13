namespace uSync.Automate.Actions.Complete.Actions.Models;

/// <summary>Result of running a uSync.Complete pipeline (publisher push/pull, restore point) to completion.</summary>
public sealed class CompleteRunOutput
{
    public bool Success { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Message { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid PipelineId { get; init; }
    public int StepCount { get; init; }
    public DateTime CompletedUtc { get; init; }
}
