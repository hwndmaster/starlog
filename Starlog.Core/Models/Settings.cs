namespace Genius.Starlog.Core.Models;

public sealed class Settings
{
    public bool AutoLoadPreviouslyOpenedProfile { get; set; } = false;
    public Guid? AutoLoadProfile { get; set; } = null;
    public ICollection<StringValue> PlainTextLogReaderLineRegexes { get; set; } = Array.Empty<StringValue>();
}

public record StringValue(string Name, string Value);
