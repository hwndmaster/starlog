using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogReading;

public sealed class XmlLogCodecProcessor : ILogCodecProcessor
{
    public Task<LogReadingResult> ReadAsync(Profile profile, FileRecord fileRecord, Stream stream, LogReadingSettings settings)
    {
        throw new NotImplementedException();
    }

    public bool ReadFromCommandLineArguments(ProfileLogCodecBase profileLogCodec, string[] codecSettings)
    {
        return true;
    }
}
