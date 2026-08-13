using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using uSync.Automate.Actions.Services;
using uSync.Automate.Actions.Complete.Processing;
using uSync.BackOffice;
using uSync.Core.Extensions;
using uSync.Publisher;

namespace uSync.Automate.Actions.Complete.Configuration;

/// <summary>
/// Actions and triggers are discovered by Automate's <c>[Action]</c>/<c>[Trigger]</c> attribute
/// scan - no explicit registration needed for those. This composer only wires up
/// <see cref="IProcessingRunner"/> and the operation gate it shares with local uSync run actions.
/// </summary>
[ComposeAfter(typeof(uSyncPublishComposer))]
public class uSyncAutomateActionsCompleteComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) => builder.AdduSyncAutomateActionsComplete();
}

public static class BuilderuSyncAutomateActionsCompleteExtensions
{
    public static IUmbracoBuilder AdduSyncAutomateActionsComplete(this IUmbracoBuilder builder)
    {
        if (builder.IsUmbracoBackOfficeEnabled() is false) return builder;

        // TryAddSingleton so this contends for the same gate as uSync.Automate.Actions' local
        // export/import, instead of creating a second, independent lock.
        builder.Services.TryAddSingleton<IuSyncOperationGate, uSyncOperationGate>();
        builder.Services.AddSingleton<IProcessingRunner, ProcessingRunner>();

        return builder;
    }
}
