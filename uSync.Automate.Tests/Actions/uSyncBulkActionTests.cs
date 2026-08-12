using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using uSync.Automate.Actions.Actions;
using uSync.Automate.Actions.Services;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.Automate.Tests.Actions;

/// <summary>Exercises uSync.report / uSync.export / uSync.import end to end through <see cref="ActionTestHarness{TAction}"/>, backed by a real <see cref="uSyncRunner"/> over mocked uSync services.</summary>
[TestFixture]
public class uSyncBulkActionTests
{
    private Mock<ISyncActionService> _actionService = null!;
    private Mock<ISyncEventService> _eventService = null!;
    private Mock<ISyncConfigService> _configService = null!;
    private uSyncRunner _runner = null!;

    [SetUp]
    public void SetUp()
    {
        _actionService = new Mock<ISyncActionService>();
        _actionService.Setup(s => s.GetActionHandlers(It.IsAny<HandlerActions>(), It.IsAny<uSyncOptions>())).Returns([]);
        _actionService.Setup(s => s.FinishProcessAsync(It.IsAny<SyncFinalActionRequest>())).ReturnsAsync(new SyncActionResult([]));
        _actionService.Setup(s => s.ImportPostAsync(It.IsAny<SyncFinalActionRequest>())).ReturnsAsync(new SyncActionResult([]));

        _eventService = new Mock<ISyncEventService>();
        _configService = new Mock<ISyncConfigService>();
        _configService.Setup(c => c.Settings).Returns(new uSyncSettings());
        _configService.Setup(c => c.GetFolders()).Returns(["uSync/v17"]);

        _runner = new uSyncRunner(_actionService.Object, _eventService.Object, _configService.Object, new uSyncOperationGate(), NullLogger<uSyncRunner>.Instance);
    }

    [Test]
    public async Task ReportAction_Succeeds()
    {
        var result = await ActionTestHarness.For<uSyncReportAction>()
            .WithService(_runner)
            .WithSettings(new uSyncReportSettings())
            .ExecuteAsync();

        Assert.That(result.Status, Is.EqualTo(ActionResultStatus.Success));
    }

    [Test]
    public async Task ExportAction_Succeeds()
    {
        var result = await ActionTestHarness.For<uSyncExportAction>()
            .WithService(_runner)
            .WithSettings(new uSyncExportSettings())
            .ExecuteAsync();

        Assert.That(result.Status, Is.EqualTo(ActionResultStatus.Success));
    }

    [Test]
    public async Task ExportAction_SkipsWhileImportIsInProgress()
    {
        _eventService.Setup(e => e.IsPaused).Returns(true);

        var result = await ActionTestHarness.For<uSyncExportAction>()
            .WithService(_runner)
            .WithSettings(new uSyncExportSettings())
            .ExecuteAsync();

        Assert.That(result.Status, Is.EqualTo(ActionResultStatus.Skipped));
        _actionService.Verify(s => s.StartProcessAsync(It.IsAny<SyncStartActionRequest>()), Times.Never);
    }

    [Test]
    public async Task ImportAction_Succeeds()
    {
        var result = await ActionTestHarness.For<uSyncImportAction>()
            .WithService(_runner)
            .WithSettings(new uSyncImportSettings())
            .ExecuteAsync();

        Assert.That(result.Status, Is.EqualTo(ActionResultStatus.Success));
    }

    [Test]
    public void ImportAction_IsMarkedAsCmsAction()
        => Assert.That(typeof(ICmsAction).IsAssignableFrom(typeof(uSyncImportAction)), Is.True);

    [Test]
    public void ExportAction_IsNotMarkedAsCmsAction()
        => Assert.That(typeof(ICmsAction).IsAssignableFrom(typeof(uSyncExportAction)), Is.False);
}
