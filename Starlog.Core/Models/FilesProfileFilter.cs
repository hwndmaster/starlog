using System.Text.Json.Serialization;

namespace Genius.Starlog.Core.Models;

/// <summary>
///   The profile filter settings for the filter which filters out log records with specified file(s).
/// </summary>
public sealed class FilesProfileFilter : ProfileFilterBase
{
    [JsonConstructor]
    public FilesProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    public FilesProfileFilter(LogFilter logFilter, Guid predefinedId)
        : base(logFilter)
    {
        Id = predefinedId;
    }

    /// <summary>
    ///   Indicates whether the selected <see cref="FileNames" /> should be included or
    ///   not when matching a log record.
    /// </summary>
    public bool Exclude { get; set; }

    /// <summary>
    ///   A list of file names to be considered in the filter.
    /// </summary>
    public string[] FileNames { get; set; } = Array.Empty<string>();
}
