namespace uSync.Automate;

/// <summary>
/// Global defaults for uSync.Automate, bound from <c>uSync:Automate</c>. Per-handler
/// <c>Settings</c> in a uSync handler set (<c>uSync:Sets:Default:Handlers:&lt;alias&gt;</c>)
/// override these when both are set - serializers read them via
/// <c>SyncSerializerOptions.GetSetting(key, default)</c> with this class's values as the default.
/// </summary>
public class uSyncAutomateOptions
{
    public const string Section = "uSync:Automate";

    /// <summary>Suppress export-on-save/publish/delete for Automate items.</summary>
    public bool DisableAutomatePush { get; set; }

    /// <summary>
    /// Omit an automation's canvas layout (viewport/positions) from the exported file. Automate
    /// rewrites this on any canvas drag, so leaving it in can produce nuisance diffs with no
    /// functional change.
    /// </summary>
    public bool IgnoreCanvasState { get; set; }

    /// <summary>
    /// Apply an automation's &lt;Status&gt; (publish/unpublish) on import. When false, every
    /// automation lands with whatever status it already has on this server, and only the
    /// definition (trigger, steps, workspace, group) is synced.
    /// </summary>
    public bool ApplyPublishedStatus { get; set; } = true;
}
