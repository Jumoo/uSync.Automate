using NUnit.Framework;
using uSync.Automate.Actions.Services;

namespace uSync.Automate.Tests.Services;

[TestFixture]
public class uSyncItemTriggerBudgetTests
{
    [Test]
    public void TryConsume_UnderCap_ReturnsTrue()
    {
        var budget = new uSyncItemTriggerBudget();
        Assert.That(budget.TryConsume(3), Is.True);
        Assert.That(budget.TryConsume(3), Is.True);
        Assert.That(budget.TryConsume(3), Is.True);
    }

    [Test]
    public void TryConsume_OverCap_ReturnsFalse()
    {
        var budget = new uSyncItemTriggerBudget();
        budget.TryConsume(2);
        budget.TryConsume(2);

        Assert.That(budget.TryConsume(2), Is.False);
    }

    [Test]
    public void TryConsume_ZeroOrNegativeMax_IsUnlimited()
    {
        var budget = new uSyncItemTriggerBudget();
        for (var i = 0; i < 1000; i++)
            Assert.That(budget.TryConsume(0), Is.True);
    }

    [Test]
    public void Reset_ClearsTheCounter()
    {
        var budget = new uSyncItemTriggerBudget();
        budget.TryConsume(1);
        Assert.That(budget.TryConsume(1), Is.False);

        budget.Reset();

        Assert.That(budget.TryConsume(1), Is.True);
    }
}
