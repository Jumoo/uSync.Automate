// Keys are named after the .NET type being synced (SyncAttempt.Succeed uses typeof(TObject).Name),
// matched by uSync's results/progress views (`uSync_{itemType}`, `uSync_group{groupName}`).
// Note the asymmetry: the CLR type is `WorkspaceGroup` but the XML root element is `AutomateGroup` -
// that's uSync's own convention (localization keys off the type name, XML off the serializer's
// ItemType), not something this package controls.
export default {
  uSync: {
    groupAutomate: "Automate",
    Automation: "Automations",
    Workspace: "Automate Workspaces",
    WorkspaceGroup: "Automate Groups",
  },
  usyncpublish: {
    Automation: "Automations",
    Workspace: "Automate Workspaces",
    WorkspaceGroup: "Automate Groups",
  },
};
