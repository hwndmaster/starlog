using System.Collections.Immutable;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.Core.LogReading;

public record LogReaderResult(ImmutableArray<LogRecord> Records, ICollection<LoggerRecord> Loggers, ICollection<LogLevelRecord> LogLevels)
{
    public static LogReaderResult Empty { get; } = new LogReaderResult(ImmutableArray<LogRecord>.Empty, Array.Empty<LoggerRecord>(), Array.Empty<LogLevelRecord>());
}
