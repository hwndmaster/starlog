using System.Text.Json.Serialization;

namespace Genius.Starlog.Core.Models;

/// <summary>
///   Used for backwards compatibility only
/// </summary>
[Obsolete("Used for backwards compatibility only. To be removed in the next major version.")]
public sealed class ThreadsProfileFilter : ProfileFilterBase
{
    [JsonConstructor]
    public ThreadsProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    public ThreadsProfileFilter(LogFilter logFilter, Guid predefinedId)
        : base(logFilter)
    {
        Id = predefinedId;
    }

    public bool Exclude { get; set; }
    public string[] Threads { get; set; } = Array.Empty<string>();
}
