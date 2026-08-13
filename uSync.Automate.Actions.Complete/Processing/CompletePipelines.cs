using uSync.Publisher.Models;

namespace uSync.Automate.Actions.Complete.Processing;

/// <summary>
/// Pipeline/strategy alias constants for uSync.Complete's Jumoo-processing pipelines.
/// </summary>
/// <remarks>
/// Restore point aliases are public upstream (<c>uSyncRestorePoints</c> in
/// <c>uSync.Expansions.Core</c>) and are referenced directly instead of being repeated here.
/// The publisher pipeline/strategy aliases are declared on <c>internal static class
/// PublisherStrategy</c> in <c>uSync.Publisher</c>, so they're re-declared here as string
/// literals - keep these in sync with that class if uSync.Publisher changes them.
/// </remarks>
public static class CompletePipelines
{
    /// <summary>The single pipeline alias every publisher strategy runs under.</summary>
    public const string PublisherPipeline = "PublisherPipeline";

    public static class PublisherStrategies
    {
        public const string RealtimePush = "RealtimePushStrategy";
        public const string RealtimePull = "RealtimePullStrategy";
    }

    /// <summary>
    /// Maps a push/pull mode onto its strategy alias. uSync.Complete.Hangfire's
    /// <c>PublisherScheduler</c> hard-codes <see cref="PublisherStrategies.RealtimePush"/> for
    /// every mode - a bug that silently runs a push strategy for a pull request. Route on
    /// <paramref name="mode"/> instead.
    /// </summary>
    public static string StrategyFor(PublishMode mode) => mode switch
    {
        PublishMode.Push => PublisherStrategies.RealtimePush,
        PublishMode.Pull => PublisherStrategies.RealtimePull,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, $"No publisher strategy for {mode}."),
    };
}
