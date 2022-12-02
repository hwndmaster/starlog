using System.Collections.Immutable;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.Core.LogReading;

public record LogReaderResult(ImmutableArray<LogRecord> Records, ICollection<LoggerRecord> Loggers, ICollection<LogLevelRecord> LogLevels);
