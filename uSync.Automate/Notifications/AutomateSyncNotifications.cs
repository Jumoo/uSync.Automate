using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace uSync.Automate.Notifications;

/// <summary>
/// Automate's own notifications (<c>AutomationSavedNotification</c>, <c>WorkspaceSavedNotification</c>,
/// etc.) derive from <see cref="ObjectNotification{T}"/>, not uSync's <see cref="SavedNotification{T}"/>
/// - and <see cref="SavedNotification{T}"/> is abstract, so it can't be constructed directly either.
/// These concrete shims let a handler forward Automate's notification into uSync's own
/// export-on-save / delete-marker / mutex logic (<c>SyncHandlerRoot.HandleAsync</c>) for the cost
/// of a one-line adapter, instead of reimplementing that logic.
/// </summary>
internal sealed class AutomateSavedNotification<T> : SavedNotification<T>
{
    public AutomateSavedNotification(T target, EventMessages messages) : base(target, messages) { }
}

internal sealed class AutomateDeletedNotification<T> : DeletedNotification<T>
{
    public AutomateDeletedNotification(T target, EventMessages messages) : base(target, messages) { }
}
