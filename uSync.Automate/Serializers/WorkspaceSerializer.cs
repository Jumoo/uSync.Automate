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
/// Serializes an Automate <see cref="Workspace"/>. Connections and user groups are recorded by
/// both Guid (fast path when the same key exists on the target) and portable alias/login name
/// (fallback when it doesn't) - the shape Automate's own Transfer API uses for connection
/// references. <see cref="Workspace.Version"/>, audit fields and dates are deliberately omitted
/// from the XML entirely: emitting them would rewrite the file on every export even when nothing
/// meaningful changed, which is worse than the alternative of blanking them in CleanseNode.
/// </summary>
[SyncSerializer("D3C1D6C1-6B23-4B0E-9E0A-1E6A8F4C7A01", "Automate Workspace Serializer", "Workspace")]
public class WorkspaceSerializer : SyncSerializerRoot<Workspace>, ISyncSerializer<Workspace>
{
    private readonly SyncAutomateService _automateService;

    public WorkspaceSerializer(ILogger<SyncSerializerRoot<Workspace>> logger, SyncAutomateService automateService)
        : base(logger)
    {
        _automateService = automateService;
    }

    protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(Workspace item, SyncSerializerOptions options)
    {
        var node = InitializeBaseNode(item, ItemAlias(item));

        var info = new XElement("Info");
        info.Add(new XElement("Name", item.Name));

        var accountName = await _automateService.GetServiceAccountUsernameAsync(item.ServiceAccountKey);
        info.Add(new XElement("ServiceAccount",
            new XAttribute("Key", item.ServiceAccountKey),
            accountName ?? string.Empty));

        node.Add(info);

        var userGroups = new XElement("UserGroups");
        var userGroupEntries = new List<(Guid Key, string? Alias)>();
        foreach (var key in item.UserGroups)
            userGroupEntries.Add((key, await _automateService.GetUserGroupAliasAsync(key)));

        foreach (var (key, alias) in userGroupEntries.OrderBy(x => x.Alias ?? x.Key.ToString(), StringComparer.Ordinal))
            userGroups.Add(new XElement("UserGroup", new XAttribute("Key", key), alias ?? string.Empty));

        node.Add(userGroups);

        var connections = new XElement("AllowedConnections");
        var connectionEntries = new List<(Guid Key, string? Alias)>();
        foreach (var key in item.AllowedConnections)
            connectionEntries.Add((key, await _automateService.GetConnectionAliasAsync(key)));

        foreach (var (key, alias) in connectionEntries.OrderBy(x => x.Alias ?? x.Key.ToString(), StringComparer.Ordinal))
            connections.Add(new XElement("Connection", new XAttribute("Key", key), alias ?? string.Empty));

        node.Add(connections);

        return SyncAttempt<XElement>.Succeed(item.Name, node, ChangeType.Export, []);
    }

    protected override async Task<SyncAttempt<Workspace>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var alias = node.GetAlias();
        var key = node.GetKey();

        var item = await FindItemAsync(key) ?? await FindItemAsync(alias);

        var info = node.Element("Info");
        var name = info?.Element("Name").ValueOrDefault(alias) ?? alias;

        item ??= new Workspace { Id = key, Alias = alias, Name = name };
        item.Alias = alias;
        item.Name = name;

        var changes = new List<uSyncChange>();

        var accountNode = info?.Element("ServiceAccount");
        if (accountNode is not null)
        {
            var accountKey = accountNode.Attribute("Key").ValueOrDefault(Guid.Empty);
            var accountName = accountNode.Value;
            var (resolvedKey, _) = await _automateService.ResolveServiceAccountAsync(accountKey, accountName);
            item.ServiceAccountKey = resolvedKey;

            if (resolvedKey == accountKey && !string.IsNullOrWhiteSpace(accountName))
            {
                // resolved by key alone - fine; nothing to warn about unless the key didn't
                // actually resolve to a user (checked inside ResolveServiceAccountAsync's caller
                // via publish validation, which is where an invalid service account surfaces).
            }
        }

        var userGroups = new List<Guid>();
        foreach (var groupNode in node.Element("UserGroups")?.Elements("UserGroup") ?? [])
        {
            var groupAlias = groupNode.Value;
            var groupKey = groupNode.Attribute("Key").ValueOrDefault(Guid.Empty);
            var resolved = await _automateService.ResolveUserGroupKeyAsync(groupAlias, groupKey);
            if (resolved is null)
            {
                changes.AddWarning("UserGroups", groupAlias, $"User group '{groupAlias}' ({groupKey}) not found on this server - dropped");
                continue;
            }

            userGroups.Add(resolved.Value);
        }

        item.UserGroups = userGroups;

        var allowedConnections = new List<Guid>();
        foreach (var connectionNode in node.Element("AllowedConnections")?.Elements("Connection") ?? [])
        {
            var connectionAlias = connectionNode.Value;
            var resolvedId = await _automateService.ResolveConnectionIdAsync(connectionAlias);
            if (resolvedId is null)
            {
                changes.AddWarning("AllowedConnections", connectionAlias, $"Connection '{connectionAlias}' not found on this server - dropped");
                continue;
            }

            allowedConnections.Add(resolvedId.Value);
        }

        item.AllowedConnections = allowedConnections;

        return SyncAttempt<Workspace>.Succeed(item.Name, item, ChangeType.Import, changes);
    }

    protected override async Task<SyncAttempt<Workspace>> ProcessDeleteAsync(Guid key, string alias, SerializerFlags flags)
    {
        var item = await FindItemAsync(key) ?? await FindItemAsync(alias);
        if (item is null) return SyncAttempt<Workspace>.Succeed(alias, ChangeType.NoChange);

        await DeleteItemAsync(item);
        return SyncAttempt<Workspace>.Succeed(alias, ChangeType.Delete);
    }

    protected override SyncAttempt<Workspace> ProcessRename(Guid key, string alias, SerializerFlags flags)
        => SyncAttempt<Workspace>.Succeed(alias, ChangeType.NoChange);

    public override Task<Workspace?> FindItemAsync(Guid key)
        => _automateService.GetWorkspaceAsync(key);

    public override Task<Workspace?> FindItemAsync(string alias)
        => _automateService.GetWorkspaceAsync(alias);

    public override Task SaveItemAsync(Workspace item)
        => _automateService.SaveWorkspaceAsync(item);

    public override async Task DeleteItemAsync(Workspace item)
        => await _automateService.DeleteWorkspaceAsync(item);

    public override string ItemAlias(Workspace item) => item.Alias;

    public override Guid ItemKey(Workspace item) => item.Id;
}
