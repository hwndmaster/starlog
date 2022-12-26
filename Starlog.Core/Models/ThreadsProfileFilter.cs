namespace Genius.Starlog.Core.Models;

/// <summary>
///   The profile filter settings for the filter which filters out log records with specified threads.
/// </summary>
public sealed class ThreadsProfileFilter : ProfileFilterBase
{
    public ThreadsProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    /// <summary>
    ///   Indicates whether the selected <see cref="Threads" /> should be included or
    ///   not when matching a log record.
    /// </summary>
    public bool Exclude { get; set; }

    /// <summary>
    ///   A list of threads to be considered in the filter.
    /// </summary>
    public string[] Threads { get; set; } = Array.Empty<string>();
}
