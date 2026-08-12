using System.Xml.Linq;
using uSync.Automate.Actions.Configuration;
using uSync.Automate.Actions.Triggers.Items;
using uSync.Core;
using uSync.Core.Extensions;

namespace uSync.Automate.Actions.Triggers.Mapping;

/// <summary>
/// Maps a uSync item-level notification onto <see cref="uSyncItemTriggerOutput"/>, applying the
/// environment-wide gate (<see cref="uSyncItemTriggerOptions"/>) that runs before the per-automation
/// settings ever see the event - <c>MapEvent</c> has no access to a subscriber's own settings, so
/// this is the only place that can stop a full-site import from writing thousands of outbox rows.
/// </summary>
public static class uSyncItemEventMapper
{
    public static uSyncItemTriggerOutput? TryMap(string operation, XElement? node, ChangeType change, uSyncItemTriggerOptions options)
    {
        if (!options.Enabled) return null;
        if (node is null) return null;

        var itemType = node.Name.LocalName;
        if (options.ItemTypes.Length > 0 && !options.ItemTypes.Contains(itemType, StringComparer.OrdinalIgnoreCase))
            return null;

        var isChange = change > ChangeType.NoChange && change < ChangeType.Hidden;
        if (options.ChangesOnly && !isChange) return null;

        var isFailure = change != ChangeType.Hidden && change >= ChangeType.Fail;

        return new uSyncItemTriggerOutput
        {
            Operation = operation,
            Change = change.ToString(),
            IsChange = isChange,
            IsFailure = isFailure,
            ItemType = itemType,
            Key = node.GetKey(),
            Alias = node.GetAlias(),
            Path = node.GetPath(),
            Level = node.GetLevel(),
            IsEmptyItem = node.IsEmptyItem(),
        };
    }
}
