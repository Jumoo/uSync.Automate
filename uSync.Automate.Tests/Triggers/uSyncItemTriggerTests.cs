using System.Xml.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using uSync.Automate.Actions.Configuration;
using uSync.Automate.Actions.Services;
using uSync.Automate.Actions.Triggers.Items;
using uSync.BackOffice;
using uSync.Core;

namespace uSync.Automate.Tests.Triggers;

[TestFixture]
public class uSyncItemTriggerTests
{
    private static TriggerInfrastructure Infrastructure()
        => new(Mock.Of<IEditableModelResolver>());

    private static IOptionsMonitor<uSyncAutomateActionsOptions> Options(uSyncItemTriggerOptions itemTriggers)
    {
        var options = new uSyncAutomateActionsOptions { ItemTriggers = itemTriggers };
        var monitor = new Mock<IOptionsMonitor<uSyncAutomateActionsOptions>>();
        monitor.Setup(m => m.CurrentValue).Returns(options);
        return monitor.Object;
    }

    private static XElement MakeNode(string itemType = "Content")
        => new(itemType, new XAttribute("Key", Guid.NewGuid()), new XAttribute("Alias", "test-item"), new XAttribute("Level", 0));

    [Test]
    public void ItemImported_MapEvent_DisabledByDefault_YieldsNothing()
    {
        var trigger = new uSyncItemImportedTrigger(
            Infrastructure(),
            Options(new uSyncItemTriggerOptions { Enabled = false }),
            new uSyncItemTriggerBudget(),
            NullLogger<uSyncItemImportedTrigger>.Instance);

        var notification = new uSyncImportedItemNotification(MakeNode(), ChangeType.Update);

        Assert.That(trigger.MapEvent(notification), Is.Empty);
    }

    [Test]
    public void ItemImported_MapEvent_WhenEnabled_YieldsEventWithPerItemIdempotencyKey()
    {
        var trigger = new uSyncItemImportedTrigger(
            Infrastructure(),
            Options(new uSyncItemTriggerOptions { Enabled = true }),
            new uSyncItemTriggerBudget(),
            NullLogger<uSyncItemImportedTrigger>.Instance);

        var node = MakeNode();
        var notification = new uSyncImportedItemNotification(node, ChangeType.Update);

        var events = trigger.MapEvent(notification).ToList();

        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].IdempotencyKey, Is.Not.Null.And.Contains("test-item"));

        var output = ((TriggerEvent<uSyncItemTriggerOutput>)events[0]).Output;
        Assert.That(output.Operation, Is.EqualTo("import"));
        Assert.That(output.IsChange, Is.True);
    }

    [Test]
    public void ItemImported_MapEvent_RespectsThePerRunCap()
    {
        var budget = new uSyncItemTriggerBudget();
        var trigger = new uSyncItemImportedTrigger(
            Infrastructure(),
            Options(new uSyncItemTriggerOptions { Enabled = true, MaxEventsPerOperation = 1 }),
            budget,
            NullLogger<uSyncItemImportedTrigger>.Instance);

        var first = trigger.MapEvent(new uSyncImportedItemNotification(MakeNode(), ChangeType.Update)).ToList();
        var second = trigger.MapEvent(new uSyncImportedItemNotification(MakeNode(), ChangeType.Update)).ToList();

        Assert.That(first, Has.Count.EqualTo(1));
        Assert.That(second, Is.Empty);
    }

    [Test]
    public void ItemExported_CanHandle_DefaultSettings_RejectsUnchangedItem()
    {
        var trigger = new uSyncItemExportedTrigger(
            Infrastructure(),
            Options(new uSyncItemTriggerOptions { Enabled = true }),
            new uSyncItemTriggerBudget(),
            NullLogger<uSyncItemExportedTrigger>.Instance);

        var output = new uSyncItemTriggerOutput { IsChange = false, ItemType = "Content" };

        Assert.That(((ITrigger)trigger).CanHandle(output, new uSyncItemTriggerSettings()), Is.False);
    }
}
