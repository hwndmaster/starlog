using System.Collections.Immutable;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.Core.LogReading;

public sealed record LogReadingResult(FileArtifacts? FileArtifacts, ImmutableArray<LogRecord> Records, ICollection<LoggerRecord> Loggers, ICollection<LogLevelRecord> LogLevels)
{
    public static LogReadingResult Empty { get; } = new LogReadingResult(null, ImmutableArray<LogRecord>.Empty, Array.Empty<LoggerRecord>(), Array.Empty<LogLevelRecord>());
}
