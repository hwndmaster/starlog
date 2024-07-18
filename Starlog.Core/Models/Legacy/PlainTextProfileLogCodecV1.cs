namespace Genius.Starlog.Core.Models.Legacy;

/// <summary>
///   Used for backwards compatibility only.
/// </summary>
[Obsolete("Used for backwards compatibility only. To be removed in the next major version.")]
public sealed class PlainTextProfileLogCodecV1
{
    public required LogCodec LogCodec { get; set; }
    public string LineRegex { get; set; } = string.Empty;
}
