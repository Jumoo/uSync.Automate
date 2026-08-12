namespace uSync.Automate.Actions.Services;

/// <summary>
/// Per-run hard cap on item-level trigger events. <c>MapEvent</c> on a trigger has no access to
/// per-automation settings - <c>CanHandle</c>, the settings-aware filter, only runs after the
/// outbox row is already written - so this is the only place a full-site import's thousands of
/// per-item notifications can be stopped from flooding the outbox if someone subscribes to an
/// item-level trigger. Reset at the start of each bulk operation by the corresponding *Starting
/// trigger.
/// </summary>
public interface IuSyncItemTriggerBudget
{
    void Reset();

    /// <summary>Returns false once <paramref name="max"/> events have been consumed since the last <see cref="Reset"/>. <paramref name="max"/> &lt;= 0 means unlimited.</summary>
    bool TryConsume(int max);
}
