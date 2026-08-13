using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core;

using uSync.Publisher.Notifications.PublisherNotifications;

namespace uSync.Automate.Actions.Complete.Triggers;

/// <summary>
/// Fires on the <em>receiving</em> site when an incoming publisher push/pull finishes -
/// <see cref="uSyncPublisherCompletedNotification"/> is only ever raised from
/// <c>uSyncReceiveApiController.Clean</c>, the endpoint the sending server calls once it's
/// finished sending. It does not fire on the sending side (that side gets its result from the
/// <c>uSyncComplete.push</c>/<c>uSyncComplete.pull</c> action instead), so this is named for
/// what it actually is: "content arrived from another server".
/// </summary>
[Trigger(
    "uSyncComplete.publishReceived",
    "uSync Complete: Publish Received",
    Description = "Fires when this site finishes receiving a push or pull from another server.",
    Group = "uSync.Complete",
    Icon = "icon-inbox",
    RequiredSections = [Constants.Applications.Settings])]
public sealed class uSyncCompletePublishReceivedTrigger
    : NotificationTriggerBase<uSyncCompletePublishReceivedSettings, uSyncCompletePublishReceivedOutput, uSyncPublisherCompletedNotification>
{
    public uSyncCompletePublishReceivedTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(uSyncPublisherCompletedNotification notification)
    {
        yield return new TriggerEvent<uSyncCompletePublishReceivedOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = TriggerInitiatorType.System,
            Output = new uSyncCompletePublishReceivedOutput
            {
                Server = notification.Request.Server,
                Mode = notification.Request.Mode.ToString(),
                CompletedUtc = DateTime.UtcNow,
            },
        };
    }

    protected override bool CanHandle(uSyncCompletePublishReceivedOutput output, uSyncCompletePublishReceivedSettings? settings)
        => string.IsNullOrWhiteSpace(settings?.Server) ||
           string.Equals(settings.Server, output.Server, StringComparison.OrdinalIgnoreCase);
}
