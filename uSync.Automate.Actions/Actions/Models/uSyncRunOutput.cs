namespace uSync.Automate.Actions.Actions.Models;

/// <summary>
/// Output of a bulk uSync operation (<c>uSync.export</c>/<c>uSync.import</c>/<c>uSync.report</c>).
/// <see cref="Changes"/> is capped at the caller's <c>MaxChangeDetails</c> setting - an
/// uncapped full-site import's change list is a multi-megabyte blob that would otherwise be
/// persisted in the WorkflowCore run row and re-serialised on every binding evaluation.
/// </summary>
public sealed class uSyncRunOutput
{
    public string Operation { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
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
