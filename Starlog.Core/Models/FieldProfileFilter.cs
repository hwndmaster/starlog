using System.Text.Json.Serialization;

namespace Genius.Starlog.Core.Models;

/// <summary>
///   The profile filter settings for the filter which filters out log records with specified fields.
/// </summary>
public sealed class FieldProfileFilter : ProfileFilterBase
{
    public static readonly Guid LogFilterId = new Guid("84a5fa19-2eeb-454a-afe9-eee8fe44bac0");

    [JsonConstructor]
    public FieldProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    public FieldProfileFilter(LogFilter logFilter, Guid predefinedId)
        : base(logFilter)
    {
        Id = predefinedId;
    }

    public required int FieldId { get; set; }

    /// <summary>
    ///   Indicates whether the selected <see cref="Values" /> should be included or
    ///   not when matching a log record.
    /// </summary>
    public bool Exclude { get; set; }

    /// <summary>
    ///   A list of field values to be considered in the filter.
    /// </summary>
    public string[] Values { get; set; } = Array.Empty<string>();
}
