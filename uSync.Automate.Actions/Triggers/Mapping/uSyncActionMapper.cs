using uSync.Automate.Actions.Actions.Models;
using uSync.BackOffice;

namespace uSync.Automate.Actions.Triggers.Mapping;

/// <summary>
/// Projects uSync's own <c>uSyncAction</c>/<c>ChangeType</c> types onto the JSON-serialisable
/// output models actions and triggers hand back to Automate. Public and static so a later
/// uSync.Complete integration (publisher push/pull, which also deals in
/// <c>IEnumerable&lt;uSyncAction&gt;</c>) can reuse it rather than duplicating the mapping.
/// </summary>
public static class uSyncActionMapper
{
    public static uSyncActionOutput ToActionOutput(uSyncAction action)
        => new()
        {
            HandlerAlias = action.HandlerAlias,
            ItemType = action.ItemType,
            Name = action.Name,
            Path = action.Path,
            Key = action.Key,
            Change = action.Change.ToString(),
            Success = action.Success,
            Message = action.Message,
            DetailMessage = action.DetailMessage,
            FileName = action.FileName,
            ExceptionMessage = action.Exception?.Message,
            DetailCount = action.Details?.Count() ?? 0,
        };

    public static uSyncHandlerSummaryOutput[] ToHandlerSummaries(IReadOnlyCollection<uSyncAction> actions)
        => actions
            .GroupBy(a => a.HandlerAlias ?? string.Empty)
            .Select(g =>
            {
                var items = g.ToList();
                return new uSyncHandlerSummaryOutput
                {
                    HandlerAlias = g.Key,
                    ItemCount = items.Count,
                    ChangeCount = items.CountChanges(),
                    ErrorCount = items.CountErrors(),
                };
            })
            .ToArray();

    public static uSyncRunOutput ToRunOutput(string operation, IReadOnlyCollection<uSyncAction> actions, int maxChangeDetails, bool success = true, string? errorMessage = null)
    {
        var actionList = actions.ToList();
        var changes = actionList.Take(Math.Max(0, maxChangeDetails)).Select(ToActionOutput).ToArray();

        return new uSyncRunOutput
        {
            Operation = operation,
            Success = success,
            ErrorMessage = errorMessage,
            ItemCount = actionList.Count,
            ChangeCount = actionList.CountChanges(),
            ErrorCount = actionList.CountErrors(),
            HasChanges = actionList.CountChanges() > 0,
            HasErrors = actionList.ContainsErrors(),
            HandlerAliases = [.. actionList.Select(a => a.HandlerAlias ?? string.Empty).Distinct()],
            Handlers = ToHandlerSummaries(actionList),
            Changes = changes,
            ChangesTruncated = actionList.Count > changes.Length,
            CompletedUtc = DateTime.UtcNow,
        };
    }
}
