namespace Genius.Starlog.Core.Models;

/// <summary>
///   Used for backwards compatibility only.
/// </summary>
[Obsolete("Used for backwards compatibility only. To be removed in the next major version.")]
public sealed class ProfileSettingsLegacy
{
    public required PlainTextProfileLogCodecV2 LogCodec { get; set; }
    public required int FileArtifactLinesCount { get; set; }
    public required string LogsLookupPattern { get; set; }
    public required string DateTimeFormat { get; set; }
}
