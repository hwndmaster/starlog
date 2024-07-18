namespace Genius.Starlog.Core.Models.Legacy;

/// <summary>
///   Used for backwards compatibility only.
/// </summary>
[Obsolete("Used for backwards compatibility only. To be removed in the next major version.")]
public sealed class PlainTextProfileSettingsV3
{
    public required LogCodec LogCodec { get; set; }
    public required string Path { get; set; }
    public Guid LinePatternId { get; set; }
    public int FileArtifactLinesCount { get; set; }
    public string? LogsLookupPattern { get; set; }
    public string? DateTimeFormat { get; set; }
}
