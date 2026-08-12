namespace uSync.Automate.Actions.Services;

/// <summary>
/// Fail-fast, in-process concurrency gate for uSync run actions. uSync's own import path holds
/// a <em>30-minute</em> internal semaphore wait (<c>SyncService</c>'s static
/// <c>_importSemaphoreLock</c>) - blocking on that from inside a WorkflowCore step would tie up
/// the step for up to half an hour before timing out. This gate is taken first, with a caller
/// controlled short wait, so a second concurrent uSync action fails immediately with a clear
/// message instead of hanging.
///
/// Public so a later uSync.Complete integration (publisher push/pull) can contend for the same
/// gate as a local export - registered with <c>TryAddSingleton</c> so a second composer doesn't
/// create a second, independent lock.
/// </summary>
public interface IuSyncOperationGate
{
    /// <summary>
    /// Attempts to acquire the gate within <paramref name="wait"/>. Returns an <see cref="IDisposable"/>
    /// lease to release it, or null if the gate could not be acquired in time.
    /// </summary>
    Task<IDisposable?> TryAcquireAsync(TimeSpan wait, CancellationToken cancellationToken);
}
