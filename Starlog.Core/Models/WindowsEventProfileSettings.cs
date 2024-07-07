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
        return new WindowsEventProfileSettings(LogCodec)
        {
            Sources = Sources,
            SelectCount = SelectCount
        };
    }

    /// <inheritdoc />
    public override string Source => string.Join(", ", Sources);

    /// <summary>
    ///   The sources to be listed.
    /// </summary>
    public string[] Sources { get; set; } = Array.Empty<string>();

    /// <summary>
    ///   Indicates the number of how many entries to select from each source.
    /// </summary>
    public int SelectCount { get; set; } = 100;
}
