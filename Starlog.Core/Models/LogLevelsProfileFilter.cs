using System.Text.Json.Serialization;

namespace Genius.Starlog.Core.Models;

/// <summary>
///   The profile filter settings for the filter which filters out log records with specified log level.
/// </summary>
public sealed class LogLevelsProfileFilter : ProfileFilterBase
{
    [JsonConstructor]
    public LogLevelsProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    public LogLevelsProfileFilter(LogFilter logFilter, Guid predefinedId)
        : base(logFilter)
    {
        Id = predefinedId;
    }

    /// <summary>
    ///   Indicates whether the selected <see cref="LogLevels" /> should be included or
    ///   not when matching a log record.
    /// </summary>
    public bool Exclude { get; set; }

    /// <summary>
    ///   A list of log levels to be considered in the filter.
    /// </summary>
    public string[] LogLevels { get; set; } = Array.Empty<string>();
}
