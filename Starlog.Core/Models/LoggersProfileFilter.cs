namespace Genius.Starlog.Core.Models;

/// <summary>
///   The profile filter settings for the filter which filters out log records with specified logger.
/// </summary>
public sealed class LoggersProfileFilter : ProfileFilterBase
{
    public LoggersProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    /// <summary>
    ///   Indicates whether the selected <see cref="LoggerNames" /> should be included or
    ///   not when matching a log record.
    /// </summary>
    public bool Exclude { get; set; }

    /// <summary>
    ///   A list of logger names to be considered in the filter.
    /// </summary>
    public string[] LoggerNames { get; set; } = Array.Empty<string>();
}
