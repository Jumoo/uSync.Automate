using Jumoo.Processing.Core.Pipelines;
using Jumoo.Processing.Core.Pipelines.Models;
using Jumoo.Processing.Core.Processing.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using uSync.Automate.Actions.Complete.Processing;
using uSync.Automate.Actions.Services;
using uSync.Publisher.Strategies.Models;

namespace uSync.Automate.Tests.Complete.Processing;

[TestFixture]
public class ProcessingRunnerTests
{
    private Mock<IPipelineService> _pipelineService = null!;
    private Mock<IUserService> _userService = null!;
    private IuSyncOperationGate _gate = null!;

    [SetUp]
    public void SetUp()
    {
        _pipelineService = new Mock<IPipelineService>();
        _userService = new Mock<IUserService>();
        _gate = new uSyncOperationGate();
    }

    private ProcessingRunner CreateRunner(IuSyncOperationGate? gate = null)
        => new(_pipelineService.Object, _userService.Object, gate ?? _gate, NullLogger<ProcessingRunner>.Instance);

    private static Pipeline PipelineWith(Guid id, PipelineStatus status) => new()
    {
        Id = id,
        Alias = "PublisherPipeline",
        Strategy = "RealtimePushStrategy",
        State = new PipelineState { Status = status },
        Options = new PublisherProcessingOptions(),
    };

    [Test]
    public async Task RunAsync_LoopsUntilCompleted()
    {
        var pipelineId = Guid.NewGuid();
        _pipelineService.Setup(p => p.CreatePipeline(It.IsAny<CreatePipelineOptions>()))
            .ReturnsAsync(PipelineWith(pipelineId, PipelineStatus.Running));
        _pipelineService.Setup(p => p.GetPipeline(pipelineId, It.IsAny<IUser?>()))
            .ReturnsAsync(PipelineWith(pipelineId, PipelineStatus.Running));

        var callCount = 0;
        _pipelineService.Setup(p => p.Process(pipelineId, It.IsAny<IUser?>(), It.IsAny<string>(), true))
            .ReturnsAsync(() =>
            {
                callCount++;
                return PipelineWith(pipelineId, callCount < 3 ? PipelineStatus.Running : PipelineStatus.Completed);
            });

        var output = await CreateRunner().RunAsync("PublisherPipeline", "RealtimePushStrategy", new PublisherProcessingOptions(), maxWaitSeconds: 5, CancellationToken.None);

        Assert.That(output.Success, Is.True);
        Assert.That(output.StepCount, Is.EqualTo(3));
        _pipelineService.Verify(p => p.Process(pipelineId, It.IsAny<IUser?>(), It.IsAny<string>(), true), Times.Exactly(3));
    }

    [Test]
    public async Task RunAsync_CreatesPipelineInBackground()
    {
        var pipelineId = Guid.NewGuid();
        _pipelineService.Setup(p => p.CreatePipeline(It.IsAny<CreatePipelineOptions>()))
            .ReturnsAsync(PipelineWith(pipelineId, PipelineStatus.Completed));
        _pipelineService.Setup(p => p.GetPipeline(pipelineId, It.IsAny<IUser?>()))
            .ReturnsAsync(PipelineWith(pipelineId, PipelineStatus.Completed));

        await CreateRunner().RunAsync("PublisherPipeline", "RealtimePushStrategy", new PublisherProcessingOptions(), maxWaitSeconds: 5, CancellationToken.None);

        _pipelineService.Verify(p => p.CreatePipeline(It.Is<CreatePipelineOptions>(o => o.IsInBackground)), Times.Once);
        _pipelineService.Verify(p => p.UpdateOptions(pipelineId, It.IsAny<IProcessingOptions>(), It.IsAny<IUser?>()), Times.Once);
    }

    [Test]
    public async Task RunAsync_PipelineFails_ReturnsFailureWithErrorMessage()
    {
        var pipelineId = Guid.NewGuid();
        _pipelineService.Setup(p => p.CreatePipeline(It.IsAny<CreatePipelineOptions>()))
            .ReturnsAsync(PipelineWith(pipelineId, PipelineStatus.Running));
        _pipelineService.Setup(p => p.GetPipeline(pipelineId, It.IsAny<IUser?>()))
            .ReturnsAsync(PipelineWith(pipelineId, PipelineStatus.Running));

        var failed = PipelineWith(pipelineId, PipelineStatus.Failed);
        failed.Results = new Jumoo.Processing.Core.Processing.Models.ProcessingResults
        {
            Error = new ProcessingErrorDetails { Source = "test", Strategy = "RealtimePushStrategy", Action = "push", Message = "server unreachable" },
        };
        _pipelineService.Setup(p => p.Process(pipelineId, It.IsAny<IUser?>(), It.IsAny<string>(), true))
            .ReturnsAsync(failed);

        var output = await CreateRunner().RunAsync("PublisherPipeline", "RealtimePushStrategy", new PublisherProcessingOptions(), maxWaitSeconds: 5, CancellationToken.None);

        Assert.That(output.Success, Is.False);
        Assert.That(output.ErrorMessage, Is.EqualTo("server unreachable"));
    }

    [Test]
    public async Task RunAsync_GateBusy_FailsFastWithoutCreatingPipeline()
    {
        var busyGate = new Mock<IuSyncOperationGate>();
        busyGate.Setup(g => g.TryAcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDisposable?)null);

        var output = await CreateRunner(busyGate.Object).RunAsync("PublisherPipeline", "RealtimePushStrategy", new PublisherProcessingOptions(), maxWaitSeconds: 5, CancellationToken.None);

        Assert.That(output.Success, Is.False);
        Assert.That(output.ErrorMessage, Does.Contain("already running"));
        _pipelineService.Verify(p => p.CreatePipeline(It.IsAny<CreatePipelineOptions>()), Times.Never);
    }

    [Test]
    public async Task RunAsync_ExceptionDuringProcessing_ReturnsFailureInsteadOfThrowing()
    {
        _pipelineService.Setup(p => p.CreatePipeline(It.IsAny<CreatePipelineOptions>()))
            .ThrowsAsync(new InvalidOperationException("no push permission"));

        var output = await CreateRunner().RunAsync("PublisherPipeline", "RealtimePushStrategy", new PublisherProcessingOptions(), maxWaitSeconds: 5, CancellationToken.None);

        Assert.That(output.Success, Is.False);
        Assert.That(output.ErrorMessage, Is.EqualTo("no push permission"));
    }

    [Test]
    public async Task RunAsync_CancelledBetweenSteps_StopsLoopingWithoutThrowing()
    {
        var pipelineId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        _pipelineService.Setup(p => p.CreatePipeline(It.IsAny<CreatePipelineOptions>()))
            .ReturnsAsync(PipelineWith(pipelineId, PipelineStatus.Running));
        _pipelineService.Setup(p => p.GetPipeline(pipelineId, It.IsAny<IUser?>()))
            .ReturnsAsync(PipelineWith(pipelineId, PipelineStatus.Running));
        _pipelineService.Setup(p => p.Process(pipelineId, It.IsAny<IUser?>(), It.IsAny<string>(), true))
            .ReturnsAsync(() =>
            {
                cts.Cancel();
                return PipelineWith(pipelineId, PipelineStatus.Running);
            });

        var output = await CreateRunner().RunAsync("PublisherPipeline", "RealtimePushStrategy", new PublisherProcessingOptions(), maxWaitSeconds: 5, cts.Token);

        Assert.That(output.Success, Is.False);
        Assert.That(output.StepCount, Is.EqualTo(1));
        _pipelineService.Verify(p => p.Process(pipelineId, It.IsAny<IUser?>(), It.IsAny<string>(), true), Times.Once);
    }
}
