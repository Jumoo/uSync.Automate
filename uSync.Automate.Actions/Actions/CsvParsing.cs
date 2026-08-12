namespace uSync.Automate.Actions.Actions;

internal static class CsvParsing
{
    public static string[]? ParseOrNull(string? csv)
        => string.IsNullOrWhiteSpace(csv)
            ? null
            : csv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
