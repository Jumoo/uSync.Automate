using uSync.Automate.Actions.Triggers.Bulk;

namespace uSync.Automate.Actions.Triggers.Mapping;

/// <summary>Shared <c>CanHandle</c> filter for the three <c>*Completed</c> bulk triggers.</summary>
public static class uSyncBulkTriggerFilter
{
    public static bool Matches(uSyncBulkTriggerOutput output, uSyncBulkTriggerSettings? settings)
    {
        if (settings is null) return true;

        if (!string.IsNullOrWhiteSpace(settings.Groups))
        {
            var groups = settings.Groups.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (output.Group is null || !groups.Contains(output.Group, StringComparer.OrdinalIgnoreCase))
                return false;
        }

        if (settings.OnlyWhenChanges && !output.HasChanges) return false;
        if (settings.OnlyWhenErrors && !output.HasErrors) return false;

        return true;
    }
}
