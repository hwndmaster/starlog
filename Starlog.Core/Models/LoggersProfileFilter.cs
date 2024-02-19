using System.Text.Json.Serialization;

namespace Genius.Starlog.Core.Models;

/// <summary>
///   Used for backwards compatibility only
/// </summary>
[Obsolete("Used for backwards compatibility only. To be removed in the next major version.")]
public sealed class LoggersProfileFilter : ProfileFilterBase
{
    [JsonConstructor]
    public LoggersProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    public LoggersProfileFilter(LogFilter logFilter, Guid predefinedId)
        : base(logFilter)
    {
        Id = predefinedId;
    }

    public bool Exclude { get; set; }
    public string[] LoggerNames { get; set; } = Array.Empty<string>();
}
