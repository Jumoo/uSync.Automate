using NUnit.Framework;
using Umbraco.Cms.Core;
using uSync.Automate.Actions.Complete.Actions;
using uSync.Publisher.Models;

namespace uSync.Automate.Tests.Complete.Actions;

[TestFixture]
public class uSyncCompleteSettingsPublishOptionsBuilderTests
{
    private static readonly Guid KeyGuid = Guid.NewGuid();

    private static uSyncCompleteSettingsPushSettings Settings() => new()
    {
        Server = "target",
        EntityType = "document-type",
        Key = KeyGuid.ToString(),
        IncludeChildren = true,
        IncludeMedia = false,
        IncludeAncestors = true,
        IncludeDependencies = true,
    };

    [Test]
    public void Build_SetsModeServerAndEntityType()
    {
        var settings = Settings();

        var options = uSyncCompleteSettingsPublishOptionsBuilder.Build(settings, PublishMode.Push);

        Assert.That(options.Mode, Is.EqualTo(PublishMode.Push));
        Assert.That(options.Server, Is.EqualTo("target"));
        Assert.That(options.EntityType, Is.EqualTo("document-type"));
    }

    [Test]
    public void Build_UsesKeyOnTheGuidUdi()
    {
        var settings = Settings();

        var options = uSyncCompleteSettingsPublishOptionsBuilder.Build(settings, PublishMode.Pull);

        Assert.That(options.Items, Has.Count.EqualTo(1));
        Assert.That(options.Items[0].Udi, Is.TypeOf<GuidUdi>());
        Assert.That(((GuidUdi)options.Items[0].Udi).Guid, Is.EqualTo(KeyGuid));
        Assert.That(((GuidUdi)options.Items[0].Udi).EntityType, Is.EqualTo("document-type"));
    }

    [Test]
    public void Build_PutsIncludeFlagsOnPublisherOptions()
    {
        var settings = Settings();

        var options = uSyncCompleteSettingsPublishOptionsBuilder.Build(settings, PublishMode.Push);

        Assert.That(options.PublisherOptions!.IncludeChildren, Is.True);
        Assert.That(options.PublisherOptions!.IncludeMedia, Is.False);
        Assert.That(options.PublisherOptions!.IncludeAncestors, Is.True);
        Assert.That(options.PublisherOptions!.IncludeDependencies, Is.True);
    }

    [Test]
    public void Build_BlankOrInvalidKey_FallsBackToGuidEmpty()
    {
        var settings = Settings();
        settings.Key = "${ trigger.doctypeKey }"; // unresolved binding, or simply blank

        var options = uSyncCompleteSettingsPublishOptionsBuilder.Build(settings, PublishMode.Push);

        Assert.That(((GuidUdi)options.Items[0].Udi).Guid, Is.EqualTo(Guid.Empty));
    }
}
