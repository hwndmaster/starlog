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
    ///   The regular expression which is used to parse each line of the log file.
    /// </summary>
    /// <remarks>
    ///   a regular expression should contain the following groups, known by the system:
    ///   - level
    ///   - datetime
    ///   - thread
    ///   - logger
    ///   - message
    /// </remarks>
    public string LineRegex { get; set; } = string.Empty;
}
