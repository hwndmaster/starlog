using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogReading;

public interface ILogCodecSettingsReader
{
    bool MayContainSourceArtifacts(ProfileSettingsBase profileSettings);
    bool ReadFromCommandLineArguments(ProfileSettingsBase profileSettings, string[] codecSettings);
}
