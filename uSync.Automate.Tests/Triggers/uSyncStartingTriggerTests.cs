using Moq;
using NUnit.Framework;
using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using uSync.Automate.Actions.Services;
using uSync.Automate.Actions.Triggers.Bulk;
using uSync.BackOffice;

namespace uSync.Automate.Tests.Triggers;

[TestFixture]
public class uSyncStartingTriggerTests
{
    private static TriggerInfrastructure Infrastructure()
        => new(Mock.Of<IEditableModelResolver>());

    [Test]
    public void ImportStarting_MapEvent_ResetsTheItemTriggerBudget()
    {
        var budget = new uSyncItemTriggerBudget();
        budget.TryConsume(1); // use it up

        var trigger = new uSyncImportStartingTrigger(Infrastructure(), budget);
        var events = trigger.MapEvent(new uSyncImportStartingNotification()).ToList();

        Assert.That(events, Has.Count.EqualTo(1));
        // A fresh budget accepts another consumption immediately after MapEvent ran.
        Assert.That(budget.TryConsume(1), Is.True);
    }

    [Test]
    public void ExportStarting_MapEvent_YieldsStartingOutput()
    {
        var trigger = new uSyncExportStartingTrigger(Infrastructure(), new uSyncItemTriggerBudget());
        var events = trigger.MapEvent(new uSyncExportStartingNotification()).ToList();

        var output = ((TriggerEvent<uSyncStartingTriggerOutput>)events[0]).Output;
        Assert.That(output.Operation, Is.EqualTo("export"));
    }

    [Test]
    public void ReportStarting_MapEvent_YieldsStartingOutput()
    {
        var trigger = new uSyncReportStartingTrigger(Infrastructure());
        var events = trigger.MapEvent(new uSyncReportStartingNotification()).ToList();

        var output = ((TriggerEvent<uSyncStartingTriggerOutput>)events[0]).Output;
        Assert.That(output.Operation, Is.EqualTo("report"));
    }
}
