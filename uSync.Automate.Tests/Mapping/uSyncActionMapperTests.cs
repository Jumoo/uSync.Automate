using NUnit.Framework;
using uSync.Automate.Actions.Triggers.Mapping;
using uSync.BackOffice;
using uSync.Core;

namespace uSync.Automate.Tests.Mapping;

[TestFixture]
public class uSyncActionMapperTests
{
    private static uSyncAction MakeAction(string handlerAlias, ChangeType change, bool success = true, string name = "item")
    {
        var action = uSyncAction.SetAction(success, name, "Content", change);
        action.HandlerAlias = handlerAlias;
        action.Key = Guid.NewGuid();
        return action;
    }

    [Test]
    public void ToActionOutput_MapsChangeAsEnumName()
    {
        var action = MakeAction("contentHandler", ChangeType.Update);

        var output = uSyncActionMapper.ToActionOutput(action);

        Assert.That(output.Change, Is.EqualTo(nameof(ChangeType.Update)));
        Assert.That(output.HandlerAlias, Is.EqualTo("contentHandler"));
        Assert.That(output.Success, Is.True);
    }

    [Test]
    public void ToActionOutput_FlattensExceptionToMessage()
    {
        var action = uSyncAction.Fail("item", "contentHandler", "Content", ChangeType.Fail, "boom", new InvalidOperationException("inner"));

        var output = uSyncActionMapper.ToActionOutput(action);

        Assert.That(output.ExceptionMessage, Is.EqualTo("inner"));
        Assert.That(output.Success, Is.False);
    }

    [Test]
    public void ToRunOutput_CountsChangesAndErrors()
    {
        var actions = new List<uSyncAction>
        {
            MakeAction("a", ChangeType.NoChange),
            MakeAction("a", ChangeType.Update),
            MakeAction("b", ChangeType.Create),
            MakeAction("b", ChangeType.Fail, success: false),
        };

        var output = uSyncActionMapper.ToRunOutput("import", actions, maxChangeDetails: 200);

        Assert.That(output.ItemCount, Is.EqualTo(4));
        Assert.That(output.ChangeCount, Is.EqualTo(3)); // Update, Create, Fail all count numerically per uSync's own CountChanges
        Assert.That(output.ErrorCount, Is.EqualTo(1));
        Assert.That(output.HasChanges, Is.True);
        Assert.That(output.HasErrors, Is.True);
        Assert.That(output.HandlerAliases, Is.EquivalentTo(new[] { "a", "b" }));
    }

    [Test]
    public void ToRunOutput_TruncatesChangesAtMaxChangeDetails()
    {
        var actions = Enumerable.Range(0, 10).Select(i => MakeAction("h", ChangeType.Update, name: $"item{i}")).ToList();

        var output = uSyncActionMapper.ToRunOutput("export", actions, maxChangeDetails: 3);

        Assert.That(output.Changes, Has.Length.EqualTo(3));
        Assert.That(output.ChangesTruncated, Is.True);
        Assert.That(output.ItemCount, Is.EqualTo(10));
    }

    [Test]
    public void ToRunOutput_CarriesFailureThrough()
    {
        var output = uSyncActionMapper.ToRunOutput("import", [], maxChangeDetails: 200, success: false, errorMessage: "gate busy");

        Assert.That(output.Success, Is.False);
        Assert.That(output.ErrorMessage, Is.EqualTo("gate busy"));
        Assert.That(output.ItemCount, Is.EqualTo(0));
    }

    [Test]
    public void ToBulkTriggerOutput_CarriesGroupThrough()
    {
        var actions = new List<uSyncAction> { MakeAction("a", ChangeType.Update) };

        var output = uSyncActionMapper.ToBulkTriggerOutput("export", "Content", actions, uSyncActionMapper.DefaultTriggerChangeCap);

        Assert.That(output.Group, Is.EqualTo("Content"));
        Assert.That(output.Operation, Is.EqualTo("export"));
        Assert.That(output.HasChanges, Is.True);
    }
}
