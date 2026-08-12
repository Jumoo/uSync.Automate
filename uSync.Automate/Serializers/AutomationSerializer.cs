using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Conditions;
using uSync.Automate.Services;
using uSync.Core;
using uSync.Core.Extensions;
using uSync.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Automate.Serializers;

/// <summary>
/// Serializes an Automate <see cref="Automation"/>. Hand-rolls element-per-property XML rather
/// than delegating to Automate's own Transfer/export API: that API stamps <c>ExportedAt</c> on
/// every call, which would rewrite every file on every export regardless of whether anything
/// changed, and its export model carries no <c>GroupId</c>/<c>Status</c>, so uSync would need
/// side-channel XML for those anyway. <see cref="Automation.Version"/> and audit fields are
/// omitted entirely for the same reason as the other serializers in this project.
///
/// Two-pass: the first pass creates/updates the automation definition; the second pass
/// (<see cref="DeserializeSecondPassAsync"/>) applies <c>&lt;Status&gt;</c> via
/// <see cref="SyncAutomateService.ApplyStatusAsync"/>, because publish validation (trigger
/// presence, service-account configuration, workspace-allowed connections, section access) can
/// legitimately fail on a fresh target - that failure should be a soft warning on the automation,
/// not a failed import of the whole item.
/// </summary>
[SyncSerializer("A4B5C6D7-8E9F-4A0B-9C1D-3E4F5A6B7C03", "Automate Automation Serializer", "Automation", IsTwoPass = true)]
public class AutomationSerializer : SyncSerializerRoot<Automation>, ISyncSerializer<Automation>
{
    private readonly SyncAutomateService _automateService;

    public AutomationSerializer(ILogger<SyncSerializerRoot<Automation>> logger, SyncAutomateService automateService)
        : base(logger)
    {
        _automateService = automateService;
    }

    protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(Automation item, SyncSerializerOptions options)
    {
        var node = InitializeBaseNode(item, ItemAlias(item));

        var info = new XElement("Info");
        info.Add(new XElement("Name", item.Name));
        info.Add(new XElement("Description", item.Description ?? string.Empty));
        info.Add(new XElement("Status", item.Status));

        var workspaceAlias = await _automateService.GetWorkspaceAliasAsync(item.WorkspaceId);
        info.Add(new XElement("Workspace", new XAttribute("Key", item.WorkspaceId), workspaceAlias ?? string.Empty));

        if (item.GroupId is Guid groupId)
        {
            var group = await _automateService.GetGroupAsync(groupId);
            var groupPath = group is null ? string.Empty : await _automateService.GetGroupPathAsync(group);
            info.Add(new XElement("Group", new XAttribute("Key", groupId), groupPath));
        }

        node.Add(info);

        if (item.Trigger is not null)
        {
            var strippedTrigger = _automateService.StripTrigger(item.Trigger);
            var triggerNode = new XElement("Trigger", new XAttribute("Alias", item.Trigger.TriggerAlias));
            triggerNode.Add(new XElement("Settings", new XCData(SyncAutomateService.ToJson(strippedTrigger?.Settings ?? []))));
            node.Add(triggerNode);
        }

        var stepsNode = new XElement("Steps");
        foreach (var step in item.Steps.OrderBy(s => s.Id))
            stepsNode.Add(await SerializeStepAsync(step));
        node.Add(stepsNode);

        var connectionsNode = new XElement("StepConnections");
        foreach (var connection in item.Connections.OrderBy(c => c.SourceStepId).ThenBy(c => c.TargetStepId))
            connectionsNode.Add(SerializeStepConnection(connection));
        node.Add(connectionsNode);

        if (item.NotificationSettings is not null)
            node.Add(new XElement("NotificationSettings", new XCData(SyncAutomateService.ToJson(item.NotificationSettings))));

        node.Add(await SerializeConnectionReferencesAsync(item));

        if (!options.GetSetting("IgnoreCanvasState", false) && item.CanvasState is not null)
            node.Add(new XElement("CanvasState", new XCData(item.CanvasState)));

        return SyncAttempt<XElement>.Succeed(item.Name, node, ChangeType.Export, []);
    }

    private async Task<XElement> SerializeStepAsync(StepConfiguration step)
    {
        var strippedSettings = _automateService.StripStepSettings(step.ActionAlias, step.Settings);
        var sensitiveFields = step.Settings.Keys.Where(k => !strippedSettings.ContainsKey(k)).OrderBy(k => k, StringComparer.Ordinal);

        var stepNode = new XElement("Step",
            new XAttribute("Id", step.Id),
            new XAttribute("ActionAlias", step.ActionAlias));

        stepNode.Add(new XElement("Name", step.Name));
        stepNode.Add(new XElement("Alias", step.Alias ?? string.Empty));

        if (step.ConnectionId is Guid connectionId)
        {
            var connectionAlias = await _automateService.GetConnectionAliasAsync(connectionId);
            stepNode.Add(new XElement("Connection", connectionAlias ?? string.Empty));
        }
        else
        {
            stepNode.Add(new XElement("Connection"));
        }

        stepNode.Add(new XElement("Position", new XAttribute("X", step.Position.X), new XAttribute("Y", step.Position.Y)));
        stepNode.Add(new XElement("ErrorBehavior", step.ErrorBehavior));
        stepNode.Add(new XElement("RetryInterval", step.RetryInterval ?? TimeSpan.Zero));
        stepNode.Add(new XElement("MaxRetries", step.MaxRetries ?? 0));
        stepNode.Add(new XElement("Settings", new XCData(SyncAutomateService.ToJson(strippedSettings))));
        stepNode.Add(new XElement("InputMappings", new XCData(SyncAutomateService.ToJson(step.InputMappings))));

        var sensitiveNode = new XElement("SensitiveFields");
        foreach (var field in sensitiveFields)
            sensitiveNode.Add(new XElement("Field", field));
        stepNode.Add(sensitiveNode);

        return stepNode;
    }

    private static XElement SerializeStepConnection(StepConnection connection)
    {
        var node = new XElement("StepConnection",
            new XAttribute("Source", connection.SourceStepId),
            new XAttribute("Target", connection.TargetStepId));

        node.Add(new XElement("SourceHandle", connection.SourceHandle ?? string.Empty));
        node.Add(new XElement("TargetHandle", connection.TargetHandle ?? string.Empty));
        node.Add(new XElement("Outcome", connection.Outcome ?? string.Empty));
        node.Add(connection.Filter is null
            ? new XElement("Filter")
            : new XElement("Filter", new XCData(SyncAutomateService.ToJson(connection.Filter))));

        return node;
    }

    private async Task<XElement> SerializeConnectionReferencesAsync(Automation item)
    {
        var node = new XElement("ConnectionReferences");

        var connectionIds = item.Steps
            .Where(s => s.ConnectionId is not null)
            .Select(s => s.ConnectionId!.Value)
            .Distinct()
            .ToList();

        var references = new List<(string Alias, string? Type, string? Name)>();
        foreach (var id in connectionIds)
        {
            var alias = await _automateService.GetConnectionAliasAsync(id);
            if (alias is null) continue;
            var (type, name) = await _automateService.GetConnectionReferenceAsync(id);
            references.Add((alias, type, name));
        }

        foreach (var (alias, type, name) in references.OrderBy(r => r.Alias, StringComparer.Ordinal))
        {
            node.Add(new XElement("Connection",
                new XAttribute("Alias", alias),
                new XAttribute("Type", type ?? string.Empty),
                new XAttribute("Name", name ?? string.Empty)));
        }

        return node;
    }

    protected override async Task<SyncAttempt<Automation>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var key = node.GetKey();
        var alias = node.GetAlias();
        var info = node.Element("Info");
        var name = info?.Element("Name").ValueOrDefault(alias) ?? alias;

        var item = await FindItemAsync(key) ?? await FindItemAsync(alias);

        var changes = new List<uSyncChange>();

        var workspaceId = await ResolveWorkspaceReferenceAsync(info?.Element("Workspace"));
        if (workspaceId is null)
        {
            var workspaceText = info?.Element("Workspace")?.Value ?? string.Empty;
            return SyncAttempt<Automation>.Fail(name, ChangeType.ImportFail,
                $"Workspace '{workspaceText}' for automation '{name}' was not found - import Automate-Workspaces first");
        }

        Guid? groupId = null;
        var groupNode = info?.Element("Group");
        if (groupNode is not null)
        {
            var groupPath = groupNode.Value;
            var group = !string.IsNullOrWhiteSpace(groupPath) ? await _automateService.GetGroupByPathAsync(groupPath) : null;
            groupId = group?.Id;

            if (groupId is null)
            {
                var groupKey = groupNode.Attribute("Key").ValueOrDefault(Guid.Empty);
                if (groupKey != Guid.Empty && await _automateService.GetGroupAsync(groupKey) is { } byKey)
                    groupId = byKey.Id;
            }

            if (groupId is null && !string.IsNullOrWhiteSpace(groupPath))
                changes.AddWarning("Group", name, $"Group '{groupPath}' not found on this server - automation created at the workspace root");
        }

        item ??= new Automation { Id = key, Alias = alias, Name = name, WorkspaceId = workspaceId.Value };
        item.Alias = alias;
        item.Name = name;
        item.Description = info?.Element("Description").ValueOrDefault(string.Empty);
        item.WorkspaceId = workspaceId.Value;
        item.GroupId = groupId;

        var triggerNode = node.Element("Trigger");
        item.Trigger = triggerNode is null
            ? null
            : new TriggerConfiguration
            {
                TriggerAlias = triggerNode.Attribute("Alias").ValueOrDefault(string.Empty),
                Settings = SyncAutomateService.DictionaryFromJson(triggerNode.Element("Settings")?.Value),
            };

        item.Steps = await DeserializeStepsAsync(node.Element("Steps"), changes);
        item.Connections = DeserializeStepConnections(node.Element("StepConnections"));

        var notificationJson = node.Element("NotificationSettings")?.Value;
        if (!string.IsNullOrWhiteSpace(notificationJson))
            item.NotificationSettings = SyncAutomateService.FromJson<AutomationNotificationSettings>(notificationJson);

        var canvasStateNode = node.Element("CanvasState");
        if (canvasStateNode is not null)
            item.CanvasState = canvasStateNode.Value;
        // else: leave item.CanvasState untouched - IgnoreCanvasState may have omitted it on
        // export, in which case overwriting an existing item's canvas layout would be wrong.

        return SyncAttempt<Automation>.Succeed(item.Name, item, ChangeType.Import, changes);
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

    private async Task<IList<StepConfiguration>> DeserializeStepsAsync(XElement? stepsNode, List<uSyncChange> changes)
    {
        var steps = new List<StepConfiguration>();
        if (stepsNode is null) return steps;

        foreach (var stepNode in stepsNode.Elements("Step"))
        {
            var stepId = stepNode.Attribute("Id").ValueOrDefault(Guid.Empty);
            var actionAlias = stepNode.Attribute("ActionAlias").ValueOrDefault(string.Empty);
            var stepName = stepNode.Element("Name").ValueOrDefault(actionAlias);

            Guid? connectionId = null;
            var connectionAlias = stepNode.Element("Connection").ValueOrDefault(string.Empty);
            if (!string.IsNullOrWhiteSpace(connectionAlias))
            {
                connectionId = await _automateService.ResolveConnectionIdAsync(connectionAlias);
                if (connectionId is null)
                    changes.AddWarning("Connection", stepName, $"Connection '{connectionAlias}' not found on this server - step '{stepName}' imported without a connection");
            }

            var positionNode = stepNode.Element("Position");

            steps.Add(new StepConfiguration
            {
                Id = stepId,
                ActionAlias = actionAlias,
                Name = stepName,
                Alias = stepNode.Element("Alias").ValueOrDefault(string.Empty) is { Length: > 0 } stepAlias ? stepAlias : null,
                ConnectionId = connectionId,
                Position = new StepPosition
                {
                    X = positionNode?.Attribute("X").ValueOrDefault(0d) ?? 0d,
                    Y = positionNode?.Attribute("Y").ValueOrDefault(0d) ?? 0d,
                },
                ErrorBehavior = stepNode.Element("ErrorBehavior").ValueOrDefault(StepErrorBehavior.Retry),
                RetryInterval = stepNode.Element("RetryInterval").ValueOrDefault(TimeSpan.Zero) is { Ticks: > 0 } interval ? interval : null,
                MaxRetries = stepNode.Element("MaxRetries").ValueOrDefault(0) is > 0 and var retries ? retries : null,
                Settings = SyncAutomateService.DictionaryFromJson(stepNode.Element("Settings")?.Value),
                InputMappings = SyncAutomateService.FromJson<Dictionary<string, string>>(stepNode.Element("InputMappings")?.Value) ?? [],
            });
        }

        return steps;
    }

    private static IList<StepConnection> DeserializeStepConnections(XElement? node)
    {
        var connections = new List<StepConnection>();
        if (node is null) return connections;

        foreach (var connectionNode in node.Elements("StepConnection"))
        {
            var filterJson = connectionNode.Element("Filter")?.Value;

            connections.Add(new StepConnection
            {
                SourceStepId = connectionNode.Attribute("Source").ValueOrDefault(Guid.Empty),
                TargetStepId = connectionNode.Attribute("Target").ValueOrDefault(Guid.Empty),
                SourceHandle = connectionNode.Element("SourceHandle").ValueOrDefault(string.Empty) is { Length: > 0 } sh ? sh : null,
                TargetHandle = connectionNode.Element("TargetHandle").ValueOrDefault(string.Empty) is { Length: > 0 } th ? th : null,
                Outcome = connectionNode.Element("Outcome").ValueOrDefault(string.Empty) is { Length: > 0 } outcome ? outcome : null,
                Filter = string.IsNullOrWhiteSpace(filterJson) ? null : SyncAutomateService.FromJson<ConditionSet>(filterJson),
            });
        }

        return connections;
    }

    public override async Task<SyncAttempt<Automation>> DeserializeSecondPassAsync(Automation item, XElement node, SyncSerializerOptions options)
    {
        if (!options.GetSetting("ApplyPublishedStatus", true))
            return SyncAttempt<Automation>.Succeed(item.Name, item, ChangeType.NoChange, []);

        var statusText = node.Element("Info")?.Element("Status").ValueOrDefault(nameof(AutomationStatus.Draft)) ?? nameof(AutomationStatus.Draft);
        if (!Enum.TryParse<AutomationStatus>(statusText, ignoreCase: true, out var status))
            status = AutomationStatus.Draft;

        var changes = new List<uSyncChange>();
        var result = await _automateService.ApplyStatusAsync(item.Id, status);
        if (!result.Success)
            changes.AddWarning("Status", item.Name, result.ErrorMessage ?? $"Could not apply status '{status}'");

        return SyncAttempt<Automation>.Succeed(item.Name, item, ChangeType.Import, changes);
    }

    protected override async Task<SyncAttempt<Automation>> ProcessDeleteAsync(Guid key, string alias, SerializerFlags flags)
    {
        var item = await FindItemAsync(key) ?? await FindItemAsync(alias);
        if (item is null) return SyncAttempt<Automation>.Succeed(alias, ChangeType.NoChange);

        await DeleteItemAsync(item);
        return SyncAttempt<Automation>.Succeed(alias, ChangeType.Delete);
    }

    protected override SyncAttempt<Automation> ProcessRename(Guid key, string alias, SerializerFlags flags)
        => SyncAttempt<Automation>.Succeed(alias, ChangeType.NoChange);

    public override Task<Automation?> FindItemAsync(Guid key)
        => _automateService.GetAutomationAsync(key);

    public override Task<Automation?> FindItemAsync(string alias)
        => _automateService.GetAutomationAsync(alias);

    public override Task SaveItemAsync(Automation item)
        => _automateService.SaveAutomationAsync(item);

    public override async Task DeleteItemAsync(Automation item)
        => await _automateService.DeleteAutomationAsync(item);

    public override string ItemAlias(Automation item) => item.Alias;

    public override Guid ItemKey(Automation item) => item.Id;
}
