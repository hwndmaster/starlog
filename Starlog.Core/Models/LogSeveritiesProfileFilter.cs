namespace Genius.Starlog.Core.Models;

/// <summary>
///   The profile filter settings for the filter which filters out log records with specified severity.
/// </summary>
public sealed class LogSeveritiesProfileFilter : ProfileFilterBase
{
    public LogSeveritiesProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    /// <summary>
    ///   Indicates whether the selected <see cref="Threads" /> should be included or
    ///   not when matching a log record.
    /// </summary>
    public bool Exclude { get; set; }

    /// <summary>
    ///   A list of severities to be considered in the filter.
    /// </summary>
    public LogSeverity[] LogSeverities { get; set; } = Array.Empty<LogSeverity>();
}
