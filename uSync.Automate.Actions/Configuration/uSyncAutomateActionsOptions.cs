namespace uSync.Automate.Actions.Configuration;

/// <summary>Global defaults for uSync.Automate.Actions, bound from <c>uSync:Automate:Actions</c>.</summary>
public class uSyncAutomateActionsOptions
{
    public const string Section = "uSync:Automate:Actions";

    /// <summary>
    /// Allow the <c>Folders</c> setting on run actions to point outside uSync's configured
    /// working folder. Off by default: a workflow author is a lower-trust role than a server
    /// admin, and this is the one real path-traversal surface these actions expose.
    /// </summary>
    public bool AllowCustomFolders { get; set; }

    public uSyncItemTriggerOptions ItemTriggers { get; set; } = new();
}

/// <summary>
/// A full-site import can fire thousands of per-item uSync notifications. Item-level triggers are
/// default-off and capped per run so subscribing to one can't flood Automate's trigger outbox -
/// the recommended pattern is a bulk *Completed trigger plus <c>umbracoAutomate.forEach</c> over
/// its capped <c>Changes</c> array instead.
/// </summary>
public class uSyncItemTriggerOptions
{
    public bool Enabled { get; set; }
    public bool ChangesOnly { get; set; } = true;
    public string[] ItemTypes { get; set; } = [];
    public int MaxEventsPerOperation { get; set; } = 500;
}
