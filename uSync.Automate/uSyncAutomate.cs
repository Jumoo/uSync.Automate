using uSync.BackOffice;
using uSync.BackOffice.Models;

namespace uSync.Automate;

/// <summary>
/// Add-on descriptor for uSync.Automate, shown in uSync's add-ons list.
/// </summary>
public class uSyncAutomate : ISyncAddOn
{
    public string Name => "AutomateEdition";

    public string Version => typeof(uSyncAutomate).Assembly.GetName().Version?.ToString() ?? "17.1.0";

    public string Icon => string.Empty;

    public string View => string.Empty;

    public string Alias => "usyncAutomate";

    public string DisplayName => "AutomateEdition";

    // after uSync.Forms (100)
    public int SortOrder => 110;

    public const string GroupName = "Automate";

    // "icon-fa-robot" doesn't exist in v17's icon registry (lucide + legacy Umbraco + simpleIcons -
    // see icon-dictionary.json shipped with @umbraco-cms/backoffice) and rendered blank. icon-flash
    // (a lightning bolt) mirrors Automate's own client, which uses it for triggers.
    public const string GroupIcon = "icon-flash";

    public const string WorkspaceEntityType = "automate-workspace";
    public const string GroupEntityType = "automate-group";
    public const string AutomationEntityType = "automate-automation";
}

/// <summary>
/// Handler priorities for uSync.Automate. All sit above <see cref="uSyncConstants.Priorites.USYNC_RESERVED_UPPER"/>
/// so third-party add-ons don't collide with uSync's own handlers, and are ordered to satisfy
/// Automate's own target-side validation (workspaces before groups before automations).
/// </summary>
public static class uSyncAutomatePriorities
{
    public const int Workspaces = uSyncConstants.Priorites.USYNC_RESERVED_UPPER + 20;
    public const int Groups = uSyncConstants.Priorites.USYNC_RESERVED_UPPER + 22;
    public const int Automations = uSyncConstants.Priorites.USYNC_RESERVED_UPPER + 25;
}
