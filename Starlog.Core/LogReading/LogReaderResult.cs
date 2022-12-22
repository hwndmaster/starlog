using System.Collections.Immutable;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.Core.LogReading;

public sealed record LogReaderResult(FileArtifacts? FileArtifacts, ImmutableArray<LogRecord> Records, ICollection<LoggerRecord> Loggers, ICollection<LogLevelRecord> LogLevels)
{
    public static LogReaderResult Empty { get; } = new LogReaderResult(null, ImmutableArray<LogRecord>.Empty, Array.Empty<LoggerRecord>(), Array.Empty<LogLevelRecord>());
}
