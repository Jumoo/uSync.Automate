using Microsoft.Extensions.DependencyInjection;
using Umbraco.Automate.Core.Notifications;
using Umbraco.Automate.Startup.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Manifest;
using uSync.Automate.Handlers;
using uSync.Automate.Services;
using uSync.BackOffice;
using uSync.Core.Extensions;

namespace uSync.Automate;

/// <summary>
/// Composes after Automate so its services are registered first (<see cref="SyncAutomateService"/>
/// depends on them) and so this composer's <c>AddNotificationAsyncHandler</c> calls run against
/// Automate's fully-configured DI container.
/// </summary>
[ComposeAfter(typeof(UmbracoAutomateComposer))]
public class uSyncAutomateComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) => builder.AdduSyncAutomate();
}

public static class BuilderuSyncAutomateExtensions
{
    public static IUmbracoBuilder AdduSyncAutomate(this IUmbracoBuilder builder)
    {
        if (builder.IsUmbracoBackOfficeEnabled() is false) return builder;

        // idempotent - short-circuits if uSync's own ISyncConfigService is already registered
        builder.AdduSync();

        builder.Services.AddSingleton<SyncAutomateService>();
        builder.Services.AddOptions<uSyncAutomateOptions>()
            .Bind(builder.Config.GetSection(uSyncAutomateOptions.Section));

        // uSyncConstants.Groups.Icons is a public mutable dictionary (uSync.BackOffice) with no
        // "Automate" entry. HandlerConfigPair.GetGroupIcon falls back to the handler's own icon
        // when the group is missing, so this is cosmetic - TryAdd so a future uSync release that
        // adds its own "Automate" entry always wins.
        uSyncConstants.Groups.Icons.TryAdd(uSyncAutomate.GroupName, uSyncAutomate.GroupIcon);

        // Export-on-save/publish/delete. Automate's notifications derive from ObjectNotification<T>,
        // not uSync's SavedNotification<T>/DeletedNotification<T> (abstract) - each handler adapts
        // via a concrete shim, see Notifications/AutomateSyncNotifications.cs.
        builder
            .AddNotificationAsyncHandler<WorkspaceSavedNotification, WorkspaceHandler>()
            .AddNotificationAsyncHandler<WorkspaceDeletedNotification, WorkspaceHandler>()
            .AddNotificationAsyncHandler<WorkspaceGroupSavedNotification, AutomateGroupHandler>()
            .AddNotificationAsyncHandler<WorkspaceGroupDeletedNotification, AutomateGroupHandler>()
            .AddNotificationAsyncHandler<AutomationSavedNotification, AutomationHandler>()
            .AddNotificationAsyncHandler<AutomationDeletedNotification, AutomationHandler>()
            .AddNotificationAsyncHandler<AutomationPublishedNotification, AutomationHandler>();

        // No Saving/Deleting (root-lock) wiring in phase 1 - the four Cancelable Saving/Deleting
        // notifications expose no public accessor for their target (Target is protected), so
        // uSync's ShouldBlockRootChangesAsync can't compute a file path to lock. See the readme's
        // "known limitations" section and the upstream Automate API asks.

        UdiParser.RegisterUdiType(uSyncAutomate.WorkspaceEntityType, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(uSyncAutomate.GroupEntityType, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(uSyncAutomate.AutomationEntityType, UdiType.GuidUdi);

        builder.Services.AddSingleton<IPackageManifestReader, uSyncAutomateManifestReader>();

        return builder;
    }
}
