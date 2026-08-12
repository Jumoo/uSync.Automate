using System.Xml.Linq;
using NUnit.Framework;
using uSync.Automate.Actions.Configuration;
using uSync.Automate.Actions.Triggers.Mapping;
using uSync.Core;

namespace uSync.Automate.Tests.Mapping;

[TestFixture]
public class uSyncItemEventMapperTests
{
    private static XElement MakeNode(string itemType = "Content")
        => new(itemType, new XAttribute("Key", Guid.NewGuid()), new XAttribute("Alias", "test-item"), new XAttribute("Level", 0));

    [Test]
    public void TryMap_Disabled_ReturnsNull()
    {
        var options = new uSyncItemTriggerOptions { Enabled = false };
        Assert.That(uSyncItemEventMapper.TryMap("import", MakeNode(), ChangeType.Update, options), Is.Null);
    }

    [Test]
    public void TryMap_NullNode_ReturnsNull()
    {
        var options = new uSyncItemTriggerOptions { Enabled = true };
        Assert.That(uSyncItemEventMapper.TryMap("import", null, ChangeType.Update, options), Is.Null);
    }

    [Test]
    public void TryMap_ItemTypeNotInAllowList_ReturnsNull()
    {
        var options = new uSyncItemTriggerOptions { Enabled = true, ItemTypes = ["DataType"] };
        Assert.That(uSyncItemEventMapper.TryMap("import", MakeNode("Content"), ChangeType.Update, options), Is.Null);
    }

    [Test]
    public void TryMap_ChangesOnly_SkipsNoChange()
    {
        var options = new uSyncItemTriggerOptions { Enabled = true, ChangesOnly = true };
        Assert.That(uSyncItemEventMapper.TryMap("import", MakeNode(), ChangeType.NoChange, options), Is.Null);
    }

    [Test]
    public void TryMap_MapsFieldsFromNode()
    {
        var options = new uSyncItemTriggerOptions { Enabled = true };
        var node = MakeNode("Content");

        var output = uSyncItemEventMapper.TryMap("export", node, ChangeType.Update, options);

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.Operation, Is.EqualTo("export"));
        Assert.That(output.ItemType, Is.EqualTo("Content"));
        Assert.That(output.Change, Is.EqualTo(nameof(ChangeType.Update)));
        Assert.That(output.IsChange, Is.True);
        Assert.That(output.IsFailure, Is.False);
        Assert.That(output.Alias, Is.EqualTo("test-item"));
    }

    [Test]
    public void TryMap_FailChangeType_MarksAsFailure()
    {
        var options = new uSyncItemTriggerOptions { Enabled = true, ChangesOnly = false };

        var output = uSyncItemEventMapper.TryMap("import", MakeNode(), ChangeType.Fail, options);

        Assert.That(output!.IsFailure, Is.True);
    }

    [Test]
    public void TryMap_HiddenChangeType_NotTreatedAsFailure()
    {
        var options = new uSyncItemTriggerOptions { Enabled = true, ChangesOnly = false };

        var output = uSyncItemEventMapper.TryMap("import", MakeNode(), ChangeType.Hidden, options);

        Assert.That(output!.IsFailure, Is.False);
    }
}
