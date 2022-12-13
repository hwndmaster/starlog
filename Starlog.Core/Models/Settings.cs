namespace Genius.Starlog.Core.Models;

public sealed class Settings
{
    public required bool AutoLoadPreviouslyOpenedProfile { get; set; }
    public Guid? AutoLoadProfile { get; set; }
}
