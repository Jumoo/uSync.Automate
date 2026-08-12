using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Cms.Core.Web;
using uSync.Automate.Actions.Actions;
using uSync.Automate.Actions.Services;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;

namespace uSync.Automate.Tests.Actions;

/// <summary>
/// Only the validation-failure paths of the two item actions are covered here - a full happy-path
/// run needs a working <see cref="IUmbracoContextFactory.EnsureUmbracoContext"/>, which returns a
/// concrete Umbraco type that isn't practical to fake outside a real Umbraco host. Validation
/// failures return before that call is made, so they're exercised without it.
/// </summary>
[TestFixture]
public class uSyncItemActionValidationTests
{
    private uSyncRunner CreateRunner()
    {
        var actionService = new Mock<ISyncActionService>();
        var eventService = new Mock<ISyncEventService>();
        var configService = new Mock<ISyncConfigService>();
        configService.Setup(c => c.Settings).Returns(new uSyncSettings());
        configService.Setup(c => c.GetFolders()).Returns(["uSync/v17"]);

        return new uSyncRunner(actionService.Object, eventService.Object, configService.Object, new uSyncOperationGate(), NullLogger<uSyncRunner>.Instance);
    }

    [Test]
    public async Task ExportItemAction_InvalidItemKey_FailsValidation()
    {
        var configService = new Mock<ISyncConfigService>();
        configService.Setup(c => c.Settings).Returns(new uSyncSettings());
        configService.Setup(c => c.GetFolders()).Returns(["uSync/v17"]);

        var result = await ActionTestHarness.For<uSyncExportItemAction>()
            .WithService(CreateRunner())
            .WithService(Mock.Of<ISyncService>())
            .WithService(configService.Object)
            .WithService(Mock.Of<IUmbracoContextFactory>())
            .WithSettings(new uSyncExportItemSettings { ItemKey = "not-a-guid" })
            .ExecuteAsync();

        Assert.That(result.Status, Is.EqualTo(ActionResultStatus.Failed));
        Assert.That(result.ErrorCategory, Is.EqualTo(StepRunErrorCategory.Validation));
    }

    [Test]
    public async Task ImportItemAction_InvalidItemKey_FailsValidation()
    {
        var result = await ActionTestHarness.For<uSyncImportItemAction>()
            .WithService(CreateRunner())
            .WithService(Mock.Of<ISyncService>())
            .WithService(Mock.Of<IUmbracoContextFactory>())
            .WithSettings(new uSyncImportItemSettings { ItemKey = "not-a-guid", HandlerAlias = "contentHandler" })
            .ExecuteAsync();

        Assert.That(result.Status, Is.EqualTo(ActionResultStatus.Failed));
        Assert.That(result.ErrorCategory, Is.EqualTo(StepRunErrorCategory.Validation));
    }

    [Test]
    public async Task ImportItemAction_MissingHandlerAlias_FailsValidation()
    {
        var result = await ActionTestHarness.For<uSyncImportItemAction>()
            .WithService(CreateRunner())
            .WithService(Mock.Of<ISyncService>())
            .WithService(Mock.Of<IUmbracoContextFactory>())
            .WithSettings(new uSyncImportItemSettings { ItemKey = Guid.NewGuid().ToString(), HandlerAlias = "" })
            .ExecuteAsync();

        Assert.That(result.Status, Is.EqualTo(ActionResultStatus.Failed));
        Assert.That(result.ErrorCategory, Is.EqualTo(StepRunErrorCategory.Validation));
    }

    [Test]
    public void ImportItemAction_IsMarkedAsCmsAction()
        => Assert.That(typeof(ICmsAction).IsAssignableFrom(typeof(uSyncImportItemAction)), Is.True);
}
