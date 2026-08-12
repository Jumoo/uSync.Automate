using Microsoft.Extensions.Logging;
using Umbraco.Automate.Core.Automations;

namespace uSync.Automate.Services;

public sealed partial class SyncAutomateService
{
    public Task<Automation?> GetAutomationAsync(Guid key)
        => RunAsync(() => _automations.GetAutomationAsync(key), nameof(GetAutomationAsync));

    public Task<Automation?> GetAutomationAsync(string alias)
        => RunAsync(() => _automations.GetAutomationByAliasAsync(alias), nameof(GetAutomationAsync));

    public Task<IEnumerable<Automation>> GetAllAutomationsAsync()
        => RunAsync(() => _automations.GetAllAutomationsAsync(), nameof(GetAllAutomationsAsync));

    /// <summary>
    /// Create-or-update the automation *definition* only - <see cref="Automation.Status"/> and
    /// <see cref="Automation.PublishedVersion"/> are deliberately left untouched here and owned
    /// by <see cref="ApplyStatusAsync"/>, which runs in the serializer's second pass.
    /// </summary>
    public async Task SaveAutomationAsync(Automation item)
    {
        var existing = await RunAsync(() => _automations.GetAutomationAsync(item.Id), nameof(SaveAutomationAsync));
        if (existing is null)
        {
            // CreateAutomationAsync honours a caller-supplied non-empty Id.
            await RunAsync(() => _automations.CreateAutomationAsync(item, null), nameof(SaveAutomationAsync));
            return;
        }

        existing.Alias = item.Alias;
        existing.Name = item.Name;
        existing.Description = item.Description;
        existing.WorkspaceId = item.WorkspaceId;
        existing.GroupId = item.GroupId;
        existing.Trigger = item.Trigger;
        existing.Steps = item.Steps;
        existing.Connections = item.Connections;
        existing.NotificationSettings = item.NotificationSettings;
        existing.CanvasState = item.CanvasState;

        await RunAsync(() => _automations.UpdateAutomationAsync(existing, null), nameof(SaveAutomationAsync));
    }

    public Task<bool> DeleteAutomationAsync(Automation item)
        => RunAsync(() => _automations.DeleteAutomationAsync(item.Id), nameof(DeleteAutomationAsync));

    /// <summary>
    /// Applies <paramref name="status"/> in the automation's second pass. Publish validation can
    /// legitimately fail on a fresh target (missing trigger, unconfigured service account, a
    /// connection not yet in the workspace's allowed list, missing section access) - this is a
    /// soft failure, not an import failure, so the definition still lands even when publishing
    /// doesn't. <see cref="AutomationStatus.Draft"/> is the default state and needs no API call.
    /// </summary>
    public async Task<SyncServiceAttempt<string>> ApplyStatusAsync(Guid id, AutomationStatus status)
    {
        try
        {
            switch (status)
            {
                case AutomationStatus.Published:
                    await RunAsync(() => _automations.PublishAutomationAsync(id, null), nameof(ApplyStatusAsync));
                    return SyncServiceAttempt<string>.Succeed("Published");

                case AutomationStatus.Unpublished:
                    await RunAsync(() => _automations.UnpublishAutomationAsync(id, null), nameof(ApplyStatusAsync));
                    return SyncServiceAttempt<string>.Succeed("Unpublished");

                case AutomationStatus.Draft:
                default:
                    return SyncServiceAttempt<string>.Succeed("Draft");
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Could not apply status {Status} to automation {Id}: {Message}", status, id, ex.Message);
            return SyncServiceAttempt<string>.Fail(ex.Message);
        }
    }

    /// <summary>Resolves a connection reference by alias, for step/workspace connection lists.</summary>
    public async Task<Guid?> ResolveConnectionIdAsync(string? alias)
    {
        if (string.IsNullOrWhiteSpace(alias)) return null;
        var connection = await RunAsync(() => _connections.GetConnectionByAliasAsync(alias), nameof(ResolveConnectionIdAsync));
        return connection?.Id;
    }

    public async Task<string?> GetConnectionAliasAsync(Guid id)
    {
        var connection = await RunAsync(() => _connections.GetConnectionAsync(id), nameof(GetConnectionAliasAsync));
        return connection?.Alias;
    }

    public async Task<(string? type, string? name)> GetConnectionReferenceAsync(Guid id)
    {
        var connection = await RunAsync(() => _connections.GetConnectionAsync(id), nameof(GetConnectionReferenceAsync));
        return connection is null ? (null, null) : (connection.Type, connection.Name);
    }
}

/// <summary>
/// Minimal result type so <see cref="SyncAutomateService.ApplyStatusAsync"/> can report a soft
/// failure without throwing. Deliberately not named <c>Attempt&lt;T&gt;</c> to avoid ambiguity
/// with <c>Umbraco.Cms.Core.Attempt&lt;T&gt;</c> in files that use both namespaces.
/// </summary>
public readonly struct SyncServiceAttempt<T>
{
    public bool Success { get; }
    public T? Result { get; }
    public string? ErrorMessage { get; }

    private SyncServiceAttempt(bool success, T? result, string? errorMessage)
    {
        Success = success;
        Result = result;
        ErrorMessage = errorMessage;
    }

    public static SyncServiceAttempt<T> Succeed(T result) => new(true, result, null);
    public static SyncServiceAttempt<T> Fail(string errorMessage) => new(false, default, errorMessage);
}
