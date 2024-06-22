using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogReading;

internal sealed class XmlLogCodecProcessor : ILogCodecProcessor
{
    public bool MayContainSourceArtifacts(ProfileSettingsBase profileSettings)
    {
        throw new NotImplementedException();
    }

    public Task<LogReadingResult> ReadAsync(Profile profile, LogSourceBase source, Stream stream, LogReadingSettings settings, ILogFieldsContainer fields)
    {
        throw new NotImplementedException();
    }

    public bool ReadFromCommandLineArguments(ProfileSettingsBase profileSettings, string[] codecSettings)
    {
        return true;
    }
}
