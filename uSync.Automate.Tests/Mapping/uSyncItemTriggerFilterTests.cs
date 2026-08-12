using NUnit.Framework;
using uSync.Automate.Actions.Triggers.Items;
using uSync.Automate.Actions.Triggers.Mapping;

namespace uSync.Automate.Tests.Mapping;

[TestFixture]
public class uSyncItemTriggerFilterTests
{
    [Test]
    public void Matches_NullSettings_AlwaysTrue()
    {
        var output = new uSyncItemTriggerOutput { ItemType = "Content" };
        Assert.That(uSyncItemTriggerFilter.Matches(output, null), Is.True);
    }

    [Test]
    public void Matches_ItemTypeFilter_RejectsNonMatchingType()
    {
        var output = new uSyncItemTriggerOutput { ItemType = "DataType" };
        var settings = new uSyncItemTriggerSettings { ItemTypes = "Content, ContentType" };

        Assert.That(uSyncItemTriggerFilter.Matches(output, settings), Is.False);
    }

    [Test]
    public void Matches_OnlyWhenChanges_Default_RejectsUnchangedItem()
    {
        var output = new uSyncItemTriggerOutput { IsChange = false };
        var settings = new uSyncItemTriggerSettings();

        Assert.That(uSyncItemTriggerFilter.Matches(output, settings), Is.False);
    }

    [Test]
    public void Matches_OnlyWhenFailed_RejectsSuccessfulItem()
    {
        var output = new uSyncItemTriggerOutput { IsChange = true, IsFailure = false };
        var settings = new uSyncItemTriggerSettings { OnlyWhenFailed = true };

        Assert.That(uSyncItemTriggerFilter.Matches(output, settings), Is.False);
    }
}
