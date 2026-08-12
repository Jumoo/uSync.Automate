using System.Text.Json;
using Umbraco.Automate.Core.Automations;

namespace uSync.Automate.Services;

public sealed partial class SyncAutomateService
{
    // Sorted keys + indented output: determinism is a hard requirement for uSync's change
    // detection (it hashes the serialized XML), not a style preference.
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
    };

    public Dictionary<string, object?> StripStepSettings(string actionAlias, Dictionary<string, object?> settings)
        => _stripper.StripStepSettings(actionAlias, settings);

    public TriggerConfiguration? StripTrigger(TriggerConfiguration? trigger)
        => _stripper.StripTrigger(trigger);

    /// <summary>Serializes a dictionary/value with sorted keys so identical data always produces identical text.</summary>
    public static string ToJson(object? value)
    {
        if (value is Dictionary<string, object?> dict)
        {
            var sorted = new SortedDictionary<string, object?>(dict, StringComparer.Ordinal);
            return JsonSerializer.Serialize(sorted, s_jsonOptions);
        }

        return JsonSerializer.Serialize(value, s_jsonOptions);
    }

    public static T? FromJson<T>(string? json)
        => string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json);

    public static Dictionary<string, object?> DictionaryFromJson(string? json)
        => string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? [];
}
