using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Automate.Core.Workspaces;
using uSync.Automate.Services;
using uSync.Core;
using uSync.Core.Extensions;
using uSync.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Automate.Serializers;

/// <summary>
/// Serializes an Automate <see cref="WorkspaceGroup"/>. <see cref="WorkspaceGroup"/> has no alias
/// of its own - its real identity is (WorkspaceId, ParentId, Name). Following the same pattern
/// uSync.Forms uses for its own folder type (<c>FolderSerializer</c>), <see cref="ItemAlias"/>
/// stays the cheap, synchronous leaf name (required because it's called from synchronous
/// contexts elsewhere in uSync), while the full workspace-qualified path - which needs async
/// service calls to build - is written to a dedicated &lt;Path&gt; element and used as the
/// fallback identity in <see cref="DeserializeCoreAsync"/> when the key doesn't match.
/// </summary>
[SyncSerializer("F1E2A3B4-7C6D-4E5F-8A9B-2C3D4E5F6A02", "Automate Group Serializer", "AutomateGroup")]
public class AutomateGroupSerializer : SyncSerializerRoot<WorkspaceGroup>, ISyncSerializer<WorkspaceGroup>
{
    private readonly SyncAutomateService _automateService;

    public AutomateGroupSerializer(ILogger<SyncSerializerRoot<WorkspaceGroup>> logger, SyncAutomateService automateService)
        : base(logger)
    {
        _automateService = automateService;
    }

    protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(WorkspaceGroup item, SyncSerializerOptions options)
    {
        var level = await _automateService.GetGroupLevelAsync(item);
        var node = InitializeBaseNode(item, ItemAlias(item), level);

        var path = await _automateService.GetGroupPathAsync(item);
        var workspaceAlias = await _automateService.GetWorkspaceAliasAsync(item.WorkspaceId);

        var info = new XElement("Info");
        info.Add(new XElement("Name", item.Name));
        info.Add(new XElement("Workspace", new XAttribute("Key", item.WorkspaceId), workspaceAlias ?? string.Empty));

        if (item.ParentId is Guid parentId)
        {
            var parent = await _automateService.GetGroupAsync(parentId);
            var parentPath = parent is null ? string.Empty : await _automateService.GetGroupPathAsync(parent);
            info.Add(new XElement("Parent", new XAttribute("Key", parentId), parentPath));
        }
        else
        {
            info.Add(new XElement("Parent"));
        }

        info.Add(new XElement("Path", path));

        node.Add(info);

        return SyncAttempt<XElement>.Succeed(item.Name, node, ChangeType.Export, []);
    }

    protected override async Task<SyncAttempt<WorkspaceGroup>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var key = node.GetKey();
        var info = node.Element("Info");
        var name = info?.Element("Name").ValueOrDefault(node.GetAlias()) ?? node.GetAlias();
        var path = info?.Element("Path").ValueOrDefault(string.Empty) ?? string.Empty;

        var item = await FindItemAsync(key);
        if (item is null && !string.IsNullOrWhiteSpace(path))
            item = await _automateService.GetGroupByPathAsync(path);

        var changes = new List<uSyncChange>();

        var workspaceNode = info?.Element("Workspace");
        var workspaceId = await ResolveWorkspaceReferenceAsync(workspaceNode);
        if (workspaceId is null)
        {
            var workspaceText = workspaceNode?.Value ?? string.Empty;
            return SyncAttempt<WorkspaceGroup>.Fail(name, ChangeType.ImportFail,
                $"Workspace '{workspaceText}' for group '{name}' was not found - import Automate-Workspaces first");
        }

        var parentId = await ResolveParentReferenceAsync(info?.Element("Parent"));
        if (info?.Element("Parent")?.Value is { Length: > 0 } && parentId is null)
        {
            changes.AddWarning("Parent", name, "Parent group not found on this server - group created at the workspace root");
        }

        item ??= new WorkspaceGroup { Id = key, Name = name, WorkspaceId = workspaceId.Value };
        item.Name = name;
        item.WorkspaceId = workspaceId.Value;
        item.ParentId = parentId;

        return SyncAttempt<WorkspaceGroup>.Succeed(item.Name, item, ChangeType.Import, changes);
    }

    private async Task<Guid?> ResolveWorkspaceReferenceAsync(XElement? workspaceNode)
    {
        if (workspaceNode is null) return null;

        var alias = workspaceNode.Value;
        var byAlias = await _automateService.ResolveWorkspaceIdAsync(alias);
        if (byAlias is not null) return byAlias;

        var key = workspaceNode.Attribute("Key").ValueOrDefault(Guid.Empty);
        if (key == Guid.Empty) return null;

        var byKey = await _automateService.GetWorkspaceAsync(key);
        return byKey?.Id;
    }

    private async Task<Guid?> ResolveParentReferenceAsync(XElement? parentNode)
    {
        if (parentNode is null) return null;

        var path = parentNode.Value;
        if (!string.IsNullOrWhiteSpace(path))
        {
            var byPath = await _automateService.GetGroupByPathAsync(path);
            if (byPath is not null) return byPath.Id;
        }

        var key = parentNode.Attribute("Key").ValueOrDefault(Guid.Empty);
        if (key == Guid.Empty) return null;

        var byKey = await _automateService.GetGroupAsync(key);
        return byKey?.Id;
    }

    protected override async Task<SyncAttempt<WorkspaceGroup>> ProcessDeleteAsync(Guid key, string alias, SerializerFlags flags)
    {
        var item = await FindItemAsync(key);
        if (item is null) return SyncAttempt<WorkspaceGroup>.Succeed(alias, ChangeType.NoChange);

        // Automate's WorkspaceGroupService cascades group deletes to child groups AND every
        // automation within them - see uSync.Automate.Tests / risk R11. A stale delete-marker
        // file for a group could wipe live automations, so this only runs when uSync's own
        // NoRemove handler setting has been explicitly overridden to allow it.
        await DeleteItemAsync(item);
        return SyncAttempt<WorkspaceGroup>.Succeed(alias, ChangeType.Delete);
    }

    protected override SyncAttempt<WorkspaceGroup> ProcessRename(Guid key, string alias, SerializerFlags flags)
        => SyncAttempt<WorkspaceGroup>.Succeed(alias, ChangeType.NoChange);

    public override Task<WorkspaceGroup?> FindItemAsync(Guid key)
        => _automateService.GetGroupAsync(key);

    /// <summary>
    /// Names aren't unique across the whole tree (only within a parent), so a bare-name lookup
    /// isn't reliable - the real fallback identity is the full path resolved from the XML's
    /// &lt;Path&gt; element in <see cref="DeserializeCoreAsync"/>, mirroring uSync.Forms'
    /// <c>FolderSerializer.FindItemAsync(string)</c>.
    /// </summary>
    public override Task<WorkspaceGroup?> FindItemAsync(string alias)
        => Task.FromResult<WorkspaceGroup?>(null);

    public override Task SaveItemAsync(WorkspaceGroup item)
        => _automateService.SaveGroupAsync(item);

    public override async Task DeleteItemAsync(WorkspaceGroup item)
        => await _automateService.DeleteGroupAsync(item);

    public override string ItemAlias(WorkspaceGroup item) => item.Name;

    public override Guid ItemKey(WorkspaceGroup item) => item.Id;
}
