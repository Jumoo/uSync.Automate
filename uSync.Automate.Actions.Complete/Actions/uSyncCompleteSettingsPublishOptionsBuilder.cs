using Umbraco.Cms.Core;

using uSync.Core.Sync;
using uSync.Publisher.Models;
using uSync.Publisher.Process.Models;
using uSync.Publisher.Strategies.Models;

namespace uSync.Automate.Actions.Complete.Actions;

/// <summary>
/// Builds a <see cref="PublisherProcessingOptions"/> from a settings push/pull settings POCO.
/// </summary>
/// <remarks>
/// Drives the same <c>RealtimePush</c>/<c>RealtimePull</c> strategy as content/media - its
/// Calculate/Dependencies/Export steps resolve items through uSync.Complete's generic
/// <c>SyncDependencyService</c>/<c>SyncItemService</c>, keyed off <c>SyncItem.Udi</c>,
/// not anything content/media-specific. Only the entity type differs: content/media picks it
/// from a dropdown, this takes any UDI entity type as free text (document types, data types,
/// templates, or a third-party item type).
/// </remarks>
internal static class uSyncCompleteSettingsPublishOptionsBuilder
{
    public static PublisherProcessingOptions Build(uSyncCompleteSettingsPublishSettingsBase settings, PublishMode mode)
    {
        var entityType = settings.EntityType.Trim();
        var key = Guid.TryParse(settings.Key, out var parsed) ? parsed : Guid.Empty;

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
                    Udi = new GuidUdi(entityType, key),
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
}
