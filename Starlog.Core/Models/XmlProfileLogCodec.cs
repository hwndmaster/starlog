namespace Genius.Starlog.Core.Models;

/// <summary>
///   The log codec settings for the profile with Xml log codec type.
/// </summary>
public sealed class XmlProfileLogCodec : ProfileLogCodecBase
{
    public XmlProfileLogCodec(LogCodec logCodec)
        : base(logCodec)
    {
    }
}
