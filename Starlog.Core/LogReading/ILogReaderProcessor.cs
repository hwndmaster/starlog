using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogReading;

public interface ILogReaderProcessor
{
    Task<IEnumerable<LogRecord>> ReadAsync(Profile profile, FileRecord fileRecord, Stream stream);
}
