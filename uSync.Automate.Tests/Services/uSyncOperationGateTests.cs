using NUnit.Framework;
using uSync.Automate.Actions.Services;

namespace uSync.Automate.Tests.Services;

[TestFixture]
public class uSyncOperationGateTests
{
    [Test]
    public async Task TryAcquireAsync_WhenFree_Succeeds()
    {
        var gate = new uSyncOperationGate();

        var lease = await gate.TryAcquireAsync(TimeSpan.Zero, CancellationToken.None);

        Assert.That(lease, Is.Not.Null);
        lease!.Dispose();
    }

    [Test]
    public async Task TryAcquireAsync_WhenHeld_FailsFastRatherThanBlocking()
    {
        var gate = new uSyncOperationGate();
        var first = await gate.TryAcquireAsync(TimeSpan.Zero, CancellationToken.None);
        Assert.That(first, Is.Not.Null);

        var second = await gate.TryAcquireAsync(TimeSpan.Zero, CancellationToken.None);

        Assert.That(second, Is.Null);
    }

    [Test]
    public async Task TryAcquireAsync_ReleasedLease_CanBeReacquired()
    {
        var gate = new uSyncOperationGate();
        var first = await gate.TryAcquireAsync(TimeSpan.Zero, CancellationToken.None);
        first!.Dispose();

        var second = await gate.TryAcquireAsync(TimeSpan.Zero, CancellationToken.None);

        Assert.That(second, Is.Not.Null);
    }

    [Test]
    public async Task TryAcquireAsync_WaitsUpToTheGivenTimeoutBeforeFailing()
    {
        var gate = new uSyncOperationGate();
        var first = await gate.TryAcquireAsync(TimeSpan.Zero, CancellationToken.None);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var second = await gate.TryAcquireAsync(TimeSpan.FromMilliseconds(200), CancellationToken.None);
        sw.Stop();

        Assert.That(second, Is.Null);
        Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(150));

        first!.Dispose();
    }
}
