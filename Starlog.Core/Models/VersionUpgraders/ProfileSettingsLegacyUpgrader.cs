using Genius.Atom.Data.Persistence;
using Genius.Starlog.Core.Models.Legacy;

namespace Genius.Starlog.Core.Models.VersionUpgraders;

public class ProfileSettingsLegacyUpgrader
    : IDataVersionUpgrader<ProfileSettingsLegacy, PlainTextProfileSettingsV3>
    , IDataVersionUpgrader<PlainTextProfileSettingsV3, PlainTextProfileSettings>
{
    public PlainTextProfileSettingsV3 Upgrade(ProfileSettingsLegacy value)
    {
        Guard.NotNull(value);

        return new PlainTextProfileSettingsV3
        {
            LogCodec = value.LogCodec.LogCodec,
            Path = string.Empty,
            LinePatternId = value.LogCodec.LinePatternId,
            FileArtifactLinesCount = value.FileArtifactLinesCount,
            LogsLookupPattern = value.LogsLookupPattern,
            DateTimeFormat = value.DateTimeFormat
        };
    }

    public PlainTextProfileSettings Upgrade(PlainTextProfileSettingsV3 value)
    {
        Guard.NotNull(value);

        return new PlainTextProfileSettings(value.LogCodec)
        {
            Paths = string.IsNullOrEmpty(value.Path) ? [] : [value.Path],
            LinePatternId = value.LinePatternId,
            FileArtifactLinesCount = value.FileArtifactLinesCount,
            LogsLookupPattern = value.LogsLookupPattern,
            DateTimeFormat = value.DateTimeFormat
        };
    }
}
