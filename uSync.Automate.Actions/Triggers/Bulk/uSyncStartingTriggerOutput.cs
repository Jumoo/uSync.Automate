namespace uSync.Automate.Actions.Triggers.Bulk;

/// <summary>
/// Output of a <c>*Starting</c> trigger. Deliberately thin: <c>CancelableuSyncBulkNotification</c>
/// carries an empty <c>Actions</c> collection and no group, and these triggers cannot cancel the
/// run they fire from (outbox dispatch is asynchronous, so setting <c>Cancel</c> on the
/// notification - even if this trigger could reach it - would have no effect by the time an
/// automation runs).
/// </summary>
public sealed class uSyncStartingTriggerOutput
{
    public string Operation { get; init; } = string.Empty;
    public DateTime StartedUtc { get; init; }
}
