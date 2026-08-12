using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Workspaces;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

namespace uSync.Automate.Trackers;

public class WorkspaceTracker : SyncXmlTrackAndMerger<Workspace>, ISyncTracker<Workspace>
{
    public WorkspaceTracker(SyncSerializerCollection serializers) : base(serializers) { }
}

public class AutomateGroupTracker : SyncXmlTrackAndMerger<WorkspaceGroup>, ISyncTracker<WorkspaceGroup>
{
    public AutomateGroupTracker(SyncSerializerCollection serializers) : base(serializers) { }
}

public class AutomationTracker : SyncXmlTrackAndMerger<Automation>, ISyncTracker<Automation>
{
    public AutomationTracker(SyncSerializerCollection serializers) : base(serializers) { }
}
