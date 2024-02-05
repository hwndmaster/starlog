using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Commands;

public abstract class ProfileUpdatableData
{
    public required string Name { get; init; }
    public required ProfileSettingsBase Settings { get; init; }
}
