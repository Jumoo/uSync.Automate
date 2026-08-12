using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using uSync.Automate.Actions.Actions;
using uSync.Automate.Actions.Services;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

namespace uSync.Automate.Tests.Actions;

[TestFixture]
public class uSyncRunnerTests
{
    private Mock<ISyncActionService> _actionService = null!;
    private Mock<ISyncEventService> _eventService = null!;
    private Mock<ISyncConfigService> _configService = null!;
    private IuSyncOperationGate _gate = null!;

    [SetUp]
    public void SetUp()
    {
        _actionService = new Mock<ISyncActionService>();
        _eventService = new Mock<ISyncEventService>();
        _configService = new Mock<ISyncConfigService>();
        _configService.Setup(c => c.Settings).Returns(new uSyncSettings());
        _configService.Setup(c => c.GetFolders()).Returns(["uSync/v17"]);
        _gate = new uSyncOperationGate();
    }

    private uSyncRunner CreateRunner(IuSyncOperationGate? gate = null)
        => new(_actionService.Object, _eventService.Object, _configService.Object, gate ?? _gate, NullLogger<uSyncRunner>.Instance);

    private static SyncHandlerView Handler(string alias) => new() { Name = alias, Alias = alias };

    private static SyncActionResult ResultWith(params uSyncAction[] actions) => new([.. actions]);

    private static uSyncAction MakeAction(string handlerAlias, ChangeType change)
    {
        var action = uSyncAction.SetAction(true, "item", "Content", change);
        action.HandlerAlias = handlerAlias;
        return action;
    }

    [Test]
    public async Task RunAsync_Export_CallsExportHandlerPerHandlerThenFinish()
    {
        _actionService.Setup(s => s.GetActionHandlers(HandlerActions.Export, It.IsAny<uSyncOptions>()))
            .Returns([Handler("contentHandler"), Handler("dataTypeHandler")]);
        _actionService.Setup(s => s.ExportHandlerAsync(It.IsAny<SyncActionOptions>(), null))
            .ReturnsAsync((SyncActionOptions o, uSyncCallbacks? _) => ResultWith(MakeAction(o.Handler!, ChangeType.Export)));
        _actionService.Setup(s => s.FinishProcessAsync(It.IsAny<SyncFinalActionRequest>()))
            .ReturnsAsync((SyncFinalActionRequest r) => new SyncActionResult([.. r.Actions]));

        var output = await CreateRunner().RunAsync(new uSyncRunRequest { Action = HandlerActions.Export }, CancellationToken.None);

        Assert.That(output.Success, Is.True);
        Assert.That(output.ItemCount, Is.EqualTo(2));
        Assert.That(output.HandlerAliases, Is.EquivalentTo(new[] { "contentHandler", "dataTypeHandler" }));

        _actionService.Verify(s => s.StartProcessAsync(It.Is<SyncStartActionRequest>(r => r.HandlerAction == HandlerActions.Export)), Times.Once);
        _actionService.Verify(s => s.ExportHandlerAsync(It.IsAny<SyncActionOptions>(), null), Times.Exactly(2));
        _actionService.Verify(s => s.ImportPostAsync(It.IsAny<SyncFinalActionRequest>()), Times.Never);
        _actionService.Verify(s => s.FinishProcessAsync(It.IsAny<SyncFinalActionRequest>()), Times.Once);
    }

    [Test]
    public async Task RunAsync_Import_CallsImportPostBeforeFinish()
    {
        _actionService.Setup(s => s.GetActionHandlers(HandlerActions.Import, It.IsAny<uSyncOptions>()))
            .Returns([Handler("contentHandler")]);
        _actionService.Setup(s => s.ImportHandlerAsync(It.IsAny<SyncActionOptions>(), null))
            .ReturnsAsync(ResultWith(MakeAction("contentHandler", ChangeType.Update)));
        _actionService.Setup(s => s.ImportPostAsync(It.IsAny<SyncFinalActionRequest>()))
            .ReturnsAsync(ResultWith(MakeAction("contentHandler", ChangeType.Update)));
        _actionService.Setup(s => s.FinishProcessAsync(It.IsAny<SyncFinalActionRequest>()))
            .ReturnsAsync(new SyncActionResult([]));

        var output = await CreateRunner().RunAsync(new uSyncRunRequest { Action = HandlerActions.Import }, CancellationToken.None);

        Assert.That(output.Success, Is.True);

        _actionService.Verify(s => s.ImportPostAsync(It.IsAny<SyncFinalActionRequest>()), Times.Once);
        _actionService.Verify(s => s.FinishProcessAsync(It.IsAny<SyncFinalActionRequest>()), Times.Once);
    }

    [Test]
    public async Task RunAsync_Report_DoesNotCallImportPost()
    {
        _actionService.Setup(s => s.GetActionHandlers(HandlerActions.Report, It.IsAny<uSyncOptions>()))
            .Returns([Handler("contentHandler")]);
        _actionService.Setup(s => s.ReportHandlerAsync(It.IsAny<SyncActionOptions>(), null))
            .ReturnsAsync(ResultWith(MakeAction("contentHandler", ChangeType.WillChange)));
        _actionService.Setup(s => s.FinishProcessAsync(It.IsAny<SyncFinalActionRequest>()))
            .ReturnsAsync(new SyncActionResult([]));

        await CreateRunner().RunAsync(new uSyncRunRequest { Action = HandlerActions.Report }, CancellationToken.None);

        _actionService.Verify(s => s.ImportPostAsync(It.IsAny<SyncFinalActionRequest>()), Times.Never);
    }

    [Test]
    public async Task RunAsync_HandlerAliasFilter_OnlyCallsMatchingHandlers()
    {
        _actionService.Setup(s => s.GetActionHandlers(HandlerActions.Export, It.IsAny<uSyncOptions>()))
            .Returns([Handler("contentHandler"), Handler("dataTypeHandler"), Handler("mediaHandler")]);
        _actionService.Setup(s => s.ExportHandlerAsync(It.IsAny<SyncActionOptions>(), null))
            .ReturnsAsync(ResultWith());
        _actionService.Setup(s => s.FinishProcessAsync(It.IsAny<SyncFinalActionRequest>()))
            .ReturnsAsync(new SyncActionResult([]));

        await CreateRunner().RunAsync(
            new uSyncRunRequest { Action = HandlerActions.Export, HandlerAliases = ["mediaHandler"] },
            CancellationToken.None);

        _actionService.Verify(s => s.ExportHandlerAsync(It.Is<SyncActionOptions>(o => o.Handler == "mediaHandler"), null), Times.Once);
        _actionService.Verify(s => s.ExportHandlerAsync(It.Is<SyncActionOptions>(o => o.Handler != "mediaHandler"), null), Times.Never);
    }

    [Test]
    public async Task RunAsync_GateBusy_FailsFastWithoutCallingActionService()
    {
        var busyGate = new Mock<IuSyncOperationGate>();
        busyGate.Setup(g => g.TryAcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDisposable?)null);

        var output = await CreateRunner(busyGate.Object).RunAsync(new uSyncRunRequest { Action = HandlerActions.Export }, CancellationToken.None);

        Assert.That(output.Success, Is.False);
        Assert.That(output.ErrorMessage, Does.Contain("already running"));
        _actionService.Verify(s => s.StartProcessAsync(It.IsAny<SyncStartActionRequest>()), Times.Never);
    }

    [Test]
    public async Task RunAsync_ExceptionDuringHandler_ReturnsFailureOutputInsteadOfThrowing()
    {
        _actionService.Setup(s => s.GetActionHandlers(HandlerActions.Export, It.IsAny<uSyncOptions>()))
            .Returns([Handler("contentHandler")]);
        _actionService.Setup(s => s.ExportHandlerAsync(It.IsAny<SyncActionOptions>(), null))
            .ThrowsAsync(new InvalidOperationException("disk full"));

        var output = await CreateRunner().RunAsync(new uSyncRunRequest { Action = HandlerActions.Export }, CancellationToken.None);

        Assert.That(output.Success, Is.False);
        Assert.That(output.ErrorMessage, Is.EqualTo("disk full"));
    }

    [Test]
    public async Task RunAsync_ReleasesTheGateEvenOnFailure()
    {
        _actionService.Setup(s => s.GetActionHandlers(HandlerActions.Export, It.IsAny<uSyncOptions>()))
            .Returns([Handler("contentHandler")]);
        _actionService.Setup(s => s.ExportHandlerAsync(It.IsAny<SyncActionOptions>(), null))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await CreateRunner().RunAsync(new uSyncRunRequest { Action = HandlerActions.Export }, CancellationToken.None);

        // If the gate lease wasn't released, a second run would fail-fast instead of proceeding.
        _actionService.Setup(s => s.FinishProcessAsync(It.IsAny<SyncFinalActionRequest>())).ReturnsAsync(new SyncActionResult([]));
        var second = await CreateRunner().RunAsync(new uSyncRunRequest { Action = HandlerActions.Export }, CancellationToken.None);

        Assert.That(second.ErrorMessage, Is.Not.EqualTo("Another uSync operation is already running."));
    }
}
