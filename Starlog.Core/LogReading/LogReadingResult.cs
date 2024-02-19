using System.Collections.Immutable;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.Core.LogReading;

internal sealed record LogReadingResult(SourceArtifacts? FileArtifacts, ImmutableArray<LogRecord> Records, ILogFieldsContainer UpdatedFields, ICollection<LogLevelRecord> LogLevels, ICollection<string> Errors)
{
    public static LogReadingResult Empty { get; } = new LogReadingResult(null, ImmutableArray<LogRecord>.Empty, new LogFieldsContainer(), Array.Empty<LogLevelRecord>(), Array.Empty<string>());
}
