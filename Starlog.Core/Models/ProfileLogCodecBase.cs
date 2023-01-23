namespace Genius.Starlog.Core.Models;

/// <summary>
///   The base class for log codecs defined in the profile with supplemental settings,
///   depending on a log codec type.
/// </summary>
public abstract class ProfileLogCodecBase
{
    protected ProfileLogCodecBase(LogCodec logCodec)
    {
        LogCodec = logCodec;
    }

    /// <summary>
    ///   Points to a log codec, registered in the system.
    /// </summary>
    public LogCodec LogCodec { get; }
}
