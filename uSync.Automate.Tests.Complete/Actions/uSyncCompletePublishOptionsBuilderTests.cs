using NUnit.Framework;
using Umbraco.Cms.Core;
using uSync.Automate.Actions.Complete.Actions;
using uSync.Automate.Actions.Complete.Processing;
using uSync.Publisher.Models;
using uSync.Publisher.Strategies.Models;

namespace uSync.Automate.Tests.Complete.Actions;

[TestFixture]
public class uSyncCompletePublishOptionsBuilderTests
{
    private static readonly Guid RootKeyGuid = Guid.NewGuid();

    private static uSyncCompletePushSettings Settings() => new()
    {
        Server = "target",
        RootKey = RootKeyGuid.ToString(),
        IncludeChildren = true,
        IncludeMedia = false,
        IncludeAncestors = true,
        IncludeDependencies = false,
    };

    [Test]
    public void Build_SetsModeAndServerAndEntityType()
    {
        var settings = Settings();
        settings.EntityType = nameof(uSyncCompleteEntityType.Media);

        var options = uSyncCompletePublishOptionsBuilder.Build(settings, PublishMode.Push);

        Assert.That(options.Mode, Is.EqualTo(PublishMode.Push));
        Assert.That(options.Server, Is.EqualTo("target"));
        Assert.That(options.EntityType, Is.EqualTo(Constants.UdiEntityType.Media));
    }

    [Test]
    public void Build_PutsIncludeFlagsOnPublisherOptionsNotOnTheItem()
    {
        // SyncPublisherPipelineProcessor.UpdateOptions derives every item's DependencyFlags from
        // PublisherOptions, overwriting whatever is set directly on the SyncItem - so the item's
        // own Flags must stay at the default (None), and the settings must land on PublisherOptions.
        var settings = Settings();

        var options = uSyncCompletePublishOptionsBuilder.Build(settings, PublishMode.Push);

        Assert.That(options.PublisherOptions!.IncludeChildren, Is.True);
        Assert.That(options.PublisherOptions!.IncludeMedia, Is.False);
        Assert.That(options.PublisherOptions!.IncludeAncestors, Is.True);
        Assert.That(options.Items, Has.Count.EqualTo(1));
        Assert.That(options.Items[0].Flags, Is.EqualTo(uSync.Core.Dependency.DependencyFlags.None));
    }

    [Test]
    public void Build_UsesRootKeyOnTheGuidUdi()
    {
        var settings = Settings();

        var options = uSyncCompletePublishOptionsBuilder.Build(settings, PublishMode.Pull);

        Assert.That(options.Items[0].Udi, Is.TypeOf<GuidUdi>());
        Assert.That(((GuidUdi)options.Items[0].Udi).Guid, Is.EqualTo(RootKeyGuid));
    }

    [Test]
    public void Build_BlankOrInvalidRootKey_FallsBackToGuidEmpty()
    {
        var settings = Settings();
        settings.RootKey = "${ trigger.contentKey }"; // unresolved binding, or simply blank

        var options = uSyncCompletePublishOptionsBuilder.Build(settings, PublishMode.Push);

        Assert.That(((GuidUdi)options.Items[0].Udi).Guid, Is.EqualTo(Guid.Empty));
    }

    [Test]
    public void StrategyFor_MapsPushAndPullToDistinctStrategies()
    {
        // Regression test for the ported uSync.Complete.Hangfire bug: PublisherScheduler hard-codes
        // the push strategy for every mode, so a pull request silently ran a push.
        var pushStrategy = CompletePipelines.StrategyFor(PublishMode.Push);
        var pullStrategy = CompletePipelines.StrategyFor(PublishMode.Pull);

        Assert.That(pushStrategy, Is.EqualTo(CompletePipelines.PublisherStrategies.RealtimePush));
        Assert.That(pullStrategy, Is.EqualTo(CompletePipelines.PublisherStrategies.RealtimePull));
        Assert.That(pushStrategy, Is.Not.EqualTo(pullStrategy));
    }
}
