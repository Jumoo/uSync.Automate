using Moq;
using NUnit.Framework;
using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using uSync.Automate.Actions.Triggers.Bulk;
using uSync.BackOffice;
using uSync.Core;

namespace uSync.Automate.Tests.Triggers;

[TestFixture]
public class uSyncBulkTriggerTests
{
    private static TriggerInfrastructure Infrastructure()
        => new(Mock.Of<IEditableModelResolver>());

    private static uSyncAction MakeAction(ChangeType change)
    {
        var action = uSyncAction.SetAction(true, "item", "Content", change);
        action.HandlerAlias = "contentHandler";
        return action;
    }

    [Test]
    public void ImportCompleted_MapEvent_YieldsOneEventWithNoIdempotencyKey()
    {
        var trigger = new uSyncImportCompletedTrigger(Infrastructure());
        var notification = new uSyncImportCompletedNotification([MakeAction(ChangeType.Update)], "Content");

        var events = trigger.MapEvent(notification).ToList();

        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].TriggerAlias, Is.EqualTo("uSync.importCompleted"));
        Assert.That(events[0].InitiatorType, Is.EqualTo(TriggerInitiatorType.System));
        // Deliberate: a hand-rolled hash would falsely collapse two separate zero-change imports.
        Assert.That(events[0].IdempotencyKey, Is.Null);

        var output = ((TriggerEvent<uSyncBulkTriggerOutput>)events[0]).Output;
        Assert.That(output.Operation, Is.EqualTo("import"));
        Assert.That(output.Group, Is.EqualTo("Content"));
        Assert.That(output.HasChanges, Is.True);
    }

    [Test]
    public void ExportCompleted_CanHandle_FiltersByGroup()
    {
        var trigger = new uSyncExportCompletedTrigger(Infrastructure());
        var output = new uSyncBulkTriggerOutput { Group = "Members" };
        var settings = new uSyncBulkTriggerSettings { Groups = "Content, Settings" };

        Assert.That(((ITrigger)trigger).CanHandle(output, settings), Is.False);
    }

    [Test]
    public void ReportCompleted_MapEvent_UsesReportOperation()
    {
        var trigger = new uSyncReportCompletedTrigger(Infrastructure());
        var notification = new uSyncReportCompletedNotification([MakeAction(ChangeType.WillChange)], null);

        var events = trigger.MapEvent(notification).ToList();
        var output = ((TriggerEvent<uSyncBulkTriggerOutput>)events[0]).Output;

        Assert.That(output.Operation, Is.EqualTo("report"));
        Assert.That(output.Group, Is.Null);
    }
}
