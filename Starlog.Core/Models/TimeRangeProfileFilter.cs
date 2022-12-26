namespace Genius.Starlog.Core.Models;

/// <summary>
///   The profile filter settings for the filter which filters out log records with specified date time.
/// </summary>
public sealed class TimeRangeProfileFilter : ProfileFilterBase
{
    public TimeRangeProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    /// <summary>
    ///   Defines the beginning time to filter out log records, taken in the specified range.
    /// </summary>
    public required DateTimeOffset TimeFrom { get; set; }

    /// <summary>
    ///   Defines the ending time to filter out log records, taken in the specified range.
    /// </summary>
    public required DateTimeOffset TimeTo { get; set; }
}
