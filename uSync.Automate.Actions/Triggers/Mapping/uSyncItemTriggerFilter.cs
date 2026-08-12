using uSync.Automate.Actions.Triggers.Items;

namespace uSync.Automate.Actions.Triggers.Mapping;

public static class uSyncItemTriggerFilter
{
    public static bool Matches(uSyncItemTriggerOutput output, uSyncItemTriggerSettings? settings)
    {
        if (settings is null) return true;

        if (!string.IsNullOrWhiteSpace(settings.ItemTypes))
        {
            var types = settings.ItemTypes.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (!types.Contains(output.ItemType, StringComparer.OrdinalIgnoreCase)) return false;
        }

        if (settings.OnlyWhenChanges && !output.IsChange) return false;
        if (settings.OnlyWhenFailed && !output.IsFailure) return false;

        return true;
    }
}
