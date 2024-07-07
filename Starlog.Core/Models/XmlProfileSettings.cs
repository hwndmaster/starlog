namespace Genius.Starlog.Core.Models;

/// <summary>
///   The log codec settings for the profile with Xml log codec type.
/// </summary>
public sealed class XmlProfileSettings : ProfileSettingsBase
{
    public const string CodecName = "XML";

    public XmlProfileSettings(LogCodec logCodec)
        : base(logCodec)
    {
    }

    internal override ProfileSettingsBase CloneInternal()
    {
        return new XmlProfileSettings(LogCodec);
    }

    public override string Source => throw new NotImplementedException();
}
