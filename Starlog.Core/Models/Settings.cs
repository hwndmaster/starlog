namespace Genius.Starlog.Core.Models;

public sealed class Settings
{
    public bool AutoLoadPreviouslyOpenedProfile { get; set; } = false;
    public Guid? AutoLoadProfile { get; set; } = null;
    public ICollection<SettingStringValue> PlainTextLogReaderLineRegexes { get; set; } = Array.Empty<SettingStringValue>();
}
