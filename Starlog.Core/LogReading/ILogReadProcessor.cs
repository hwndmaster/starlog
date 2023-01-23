using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogReading;

public interface ILogCodecProcessor
{
    Task<LogReadingResult> ReadAsync(Profile profile, FileRecord fileRecord, Stream stream, LogReadingSettings settings);
    bool ReadFromCommandLineArguments(ProfileLogCodecBase profileLogCodec, string[] codecSettings);
}
