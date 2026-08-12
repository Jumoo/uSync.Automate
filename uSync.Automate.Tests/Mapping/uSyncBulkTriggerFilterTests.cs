using NUnit.Framework;
using uSync.Automate.Actions.Triggers.Bulk;
using uSync.Automate.Actions.Triggers.Mapping;

namespace uSync.Automate.Tests.Mapping;

[TestFixture]
public class uSyncBulkTriggerFilterTests
{
    [Test]
    public void Matches_NullSettings_AlwaysTrue()
    {
        var output = new uSyncBulkTriggerOutput { Group = "Content", HasChanges = false, HasErrors = false };
        Assert.That(uSyncBulkTriggerFilter.Matches(output, null), Is.True);
    }

    [Test]
    public void Matches_GroupFilter_RejectsNonMatchingGroup()
    {
        var output = new uSyncBulkTriggerOutput { Group = "Content" };
        var settings = new uSyncBulkTriggerSettings { Groups = "Settings, Forms" };

        Assert.That(uSyncBulkTriggerFilter.Matches(output, settings), Is.False);
    }

    [Test]
    public void Matches_GroupFilter_AcceptsMatchingGroupCaseInsensitive()
    {
        var output = new uSyncBulkTriggerOutput { Group = "content" };
        var settings = new uSyncBulkTriggerSettings { Groups = "Settings, Content" };

        Assert.That(uSyncBulkTriggerFilter.Matches(output, settings), Is.True);
    }

    [Test]
    public void Matches_OnlyWhenChanges_RejectsNoChangeRun()
    {
        var output = new uSyncBulkTriggerOutput { HasChanges = false };
        var settings = new uSyncBulkTriggerSettings { OnlyWhenChanges = true };

        Assert.That(uSyncBulkTriggerFilter.Matches(output, settings), Is.False);
    }

    [Test]
    public void Matches_OnlyWhenErrors_RejectsCleanRun()
    {
        var output = new uSyncBulkTriggerOutput { HasErrors = false };
        var settings = new uSyncBulkTriggerSettings { OnlyWhenErrors = true };

        Assert.That(uSyncBulkTriggerFilter.Matches(output, settings), Is.False);
    }
}
