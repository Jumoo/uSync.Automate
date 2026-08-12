using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using uSync.Automate.Actions.Actions;
using uSync.Automate.Actions.Services;
using uSync.BackOffice;
using uSync.Core.Extensions;

namespace uSync.Automate.Actions.Configuration;

/// <summary>
/// Actions and triggers are discovered by Umbraco's <c>TypeLoader</c> attribute scan (Automate's
/// own <c>[Action]</c>/<c>[Trigger]</c> scan, same as <c>[SyncHandler]</c>/<c>[SyncSerializer]</c>
/// for uSync) - no explicit registration needed for those. This composer only wires up the extra
/// constructor dependencies they share: <see cref="uSyncRunner"/> and the operation gate.
///
/// No <c>[ComposeAfter(typeof(UmbracoAutomateComposer))]</c> here - this project references only
/// <c>Umbraco.Automate.Core</c> (Automate's stable extensibility surface), and that composer type
/// lives in <c>Umbraco.Automate.Startup</c>, which is deliberately not referenced. Nothing this
/// composer registers depends on Automate's own composition order.
/// </summary>
public class uSyncAutomateActionsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) => builder.AdduSyncAutomateActions();
}

public static class BuilderuSyncAutomateActionsExtensions
{
    public static IUmbracoBuilder AdduSyncAutomateActions(this IUmbracoBuilder builder)
    {
        if (builder.IsUmbracoBackOfficeEnabled() is false) return builder;

        builder.AdduSync();

        // TryAddSingleton so a later uSync.Complete integration contends for the same gate as a
        // local export instead of creating a second, independent lock.
        builder.Services.TryAddSingleton<IuSyncOperationGate, uSyncOperationGate>();
        builder.Services.AddSingleton<uSyncRunner>();

        builder.Services.AddOptions<uSyncAutomateActionsOptions>()
            .Bind(builder.Config.GetSection(uSyncAutomateActionsOptions.Section));

        return builder;
    }
}
