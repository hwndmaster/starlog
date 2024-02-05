namespace Genius.Starlog.Core.Models;

/// <summary>
///   The log codec settings for the profile with Windows Event log codec type.
/// </summary>
public sealed class WindowsEventProfileSettings : ProfileSettingsBase
{
    public const string CodecName = "Windows Events";

    public WindowsEventProfileSettings(LogCodec logCodec)
        : base(logCodec)
    {
    }

    internal override ProfileSettingsBase CloneInternal()
    {
        return new WindowsEventProfileSettings(LogCodec);
    }

    public override string Source => throw new NotImplementedException();
}
