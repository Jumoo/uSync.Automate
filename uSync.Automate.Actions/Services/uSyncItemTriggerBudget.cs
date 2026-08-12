namespace uSync.Automate.Actions.Services;

public sealed class uSyncItemTriggerBudget : IuSyncItemTriggerBudget
{
    private int _count;

    public void Reset() => Interlocked.Exchange(ref _count, 0);

    public bool TryConsume(int max)
    {
        if (max <= 0) return true;
        return Interlocked.Increment(ref _count) <= max;
    }
}
