using Umbraco.Cms.Core;

using uSync.Core.Sync;
using uSync.Publisher.Models;
using uSync.Publisher.Process.Models;
using uSync.Publisher.Strategies.Models;

namespace uSync.Automate.Actions.Complete.Actions;

/// <summary>
/// Builds a <see cref="PublisherProcessingOptions"/> from a push/pull settings POCO.
/// </summary>
/// <remarks>
/// <c>uSync.Complete.Hangfire</c>'s <c>PublisherScheduler</c> forces
/// <c>IncludeChildren/Media/Ancestors</c> to <c>true</c> and ignores the flags it's given -
/// build <see cref="SyncPublisherOptions"/> from the caller's settings instead, since
/// <c>SyncPublisherPipelineProcessor.UpdateOptions</c> derives every item's dependency flags
/// from this object, not from flags set directly on the <see cref="SyncItem"/>.
/// </remarks>
internal static class uSyncCompletePublishOptionsBuilder
{
    public static PublisherProcessingOptions Build(uSyncCompletePublishSettingsBase settings, PublishMode mode)
    {
        var entityType = uSyncCompleteEntityTypeEditor.Parse(settings.EntityType) switch
        {
            uSyncCompleteEntityType.Media => Constants.UdiEntityType.Media,
            _ => Constants.UdiEntityType.Document,
        };

        return new PublisherProcessingOptions
        {
            Mode = mode,
            Server = settings.Server,
            EntityType = entityType,
            IsBackgroundRequest = true,
            CreateRestorePoint = settings.CreateRestorePoint,
            Items =
            [
                new SyncItem
                {
                    Name = "Automate start point",
                    Udi = new GuidUdi(entityType, ParseRootKey(settings.RootKey)),
                    Icon = "icon-document",
                }
            ],
            PublisherOptions = new SyncPublisherOptions
            {
                IncludeChildren = settings.IncludeChildren,
                IncludeMedia = settings.IncludeMedia,
                IncludeAncestors = settings.IncludeAncestors,
                IncludeDependencies = settings.IncludeDependencies,
                IncludeFileHash = true,
            },
        };
    }

    /// <summary>
    /// <c>RootKey</c> is a <c>string</c>, not a <c>Guid</c>, because <c>SettingsBindingResolver</c> only
    /// resolves <c>${ }</c> bindings on <c>string</c> (and <c>IList&lt;string&gt;</c>) properties - a
    /// <c>Guid</c>-typed field never gets its binding substituted, so a value like
    /// <c>${ trigger.contentKey }</c> would reach JSON deserialization unresolved and throw.
    /// </summary>
    private static Guid ParseRootKey(string? rootKey)
        => Guid.TryParse(rootKey, out var key) ? key : Guid.Empty;
}
