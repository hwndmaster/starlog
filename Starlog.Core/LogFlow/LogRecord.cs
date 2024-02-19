using System.Collections.Immutable;

namespace Genius.Starlog.Core.LogFlow;

/// <summary>
///   A record contains information regarding a log record.
/// </summary>
public readonly record struct LogRecord(DateTimeOffset DateTime, LogLevelRecord Level, LogSourceBase Source, ImmutableArray<int> FieldValueIndices, string Message, string? LogArtifacts);
