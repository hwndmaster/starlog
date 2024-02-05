using Genius.Atom.Data.Persistence;

namespace Genius.Starlog.Core.Models.VersionUpgraders;

public class ProfileSettingsLegacyUpgrader : IDataVersionUpgrader<ProfileSettingsLegacy, PlainTextProfileSettings>
{
    public PlainTextProfileSettings Upgrade(ProfileSettingsLegacy value)
    {
        return new PlainTextProfileSettings(value.LogCodec.LogCodec)
        {
            Path = string.Empty,
            LinePatternId = value.LogCodec.LinePatternId,
            FileArtifactLinesCount = value.FileArtifactLinesCount,
            LogsLookupPattern = value.LogsLookupPattern,
            DateTimeFormat = value.DateTimeFormat
        };
    }
}
