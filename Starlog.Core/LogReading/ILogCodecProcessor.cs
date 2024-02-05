using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogReading;

public interface ILogCodecProcessor
{
    Task<LogReadingResult> ReadAsync(Profile profile, LogSourceBase source, Stream stream, LogReadingSettings settings);
    bool ReadFromCommandLineArguments(ProfileSettingsBase profileSettings, string[] codecSettings);
    bool MayContainSourceArtifacts(ProfileSettingsBase profileSettings);
}
