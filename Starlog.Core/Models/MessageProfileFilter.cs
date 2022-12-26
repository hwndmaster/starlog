namespace Genius.Starlog.Core.Models;

/// <summary>
///   The profile filter settings for the filter which filters out log records with specified message and/or artifacts.
/// </summary>
public sealed class MessageProfileFilter : ProfileFilterBase
{
    public MessageProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    /// <summary>
    ///   The search pattern.
    /// </summary>
    public required string Pattern { get; set; }

    /// <summary>
    ///   Indicates whether the filter should use Regular Expressions when matching a log record.
    /// </summary>
    public bool IsRegex { get; set; }

    /// <summary>
    ///   Indicates whether the casing should be considered when matching a log record.
    /// </summary>
    public bool MatchCasing { get; set; }

    /// <summary>
    ///   Indicates whether the filter should also try to match the <see cref="LogFlow.LogRecord.LogArtifacts"/> value.
    /// </summary>
    public bool IncludeArtifacts { get; set; }
}
