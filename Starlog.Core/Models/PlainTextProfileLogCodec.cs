namespace Genius.Starlog.Core.Models;

/// <summary>
///   The log codec settings for the profile with Plain Text log codec type.
/// </summary>
public sealed class PlainTextProfileLogCodec : ProfileLogCodecBase
{
    public PlainTextProfileLogCodec(LogCodec logCodec)
        : base(logCodec)
    {
    }

    /// <summary>
    ///   The pattern which is used to parse each line of the log file.
    /// </summary>
    public Guid LinePatternId { get; set; }
}

/// <summary>
///   The log codec settings for the profile with Plain Text log codec type.
/// </summary>
public sealed class PlainTextProfileLogCodecLegacy : ProfileLogCodecBase
{
    public PlainTextProfileLogCodecLegacy(LogCodec logCodec)
        : base(logCodec)
    {
    }

    public string LineRegex { get; set; } = string.Empty;
}
