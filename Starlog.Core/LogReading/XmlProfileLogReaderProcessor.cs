using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogReading;

public sealed class XmlProfileLogReaderProcessor : ILogReaderProcessor
{
    public Task<LogReaderResult> ReadAsync(Profile profile, FileRecord fileRecord, Stream stream, LogReadingSettings settings)
    {
        throw new NotImplementedException();
    }
}
