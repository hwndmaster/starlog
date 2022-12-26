using System.Text.Json.Serialization;

namespace Genius.Starlog.Core.Models;

/// <summary>
///   The profile filter settings for the filter which filters out log records with specified date time.
/// </summary>
public sealed class TimeAgoProfileFilter : ProfileFilterBase
{
    public TimeAgoProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    /// <summary>
    ///   Defines the time span to filter out log records, taken in the past <see cref="TimeAgo" />.
    /// </summary>
    [JsonIgnore]
    public TimeSpan TimeAgo { get; set; }

    /// <summary>
    ///   Defines the milliseconds to filter out log records, taken in the past <see cref="MillisecondsAgo" /> ms.
    ///   Repeats the same value as <see cref="TimeAgo" /> for serialization purposes.
    /// </summary>
    public ulong MillisecondsAgo
    {
        get => (ulong)TimeAgo.TotalMilliseconds;
        set => TimeAgo = TimeSpan.FromMilliseconds(value);
    }
}
