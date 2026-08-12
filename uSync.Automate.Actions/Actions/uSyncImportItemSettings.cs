using Umbraco.Automate.Core.Settings;

namespace uSync.Automate.Actions.Actions;

public sealed class uSyncImportItemSettings
{
    [Field(Label = "Item Key", Description = "The Guid key of the item to import.", SupportsBindings = true)]
    public string ItemKey { get; set; } = string.Empty;

    [Field(Label = "Handler Alias", Description = "The uSync handler alias that owns this item, e.g. contentHandler.", SortOrder = 1)]
    public string HandlerAlias { get; set; } = string.Empty;
}
