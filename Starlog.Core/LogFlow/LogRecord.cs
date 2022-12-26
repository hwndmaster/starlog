namespace Genius.Starlog.Core.LogFlow;

/// <summary>
///   A record contains information regarding a log record.
/// </summary>
public readonly record struct LogRecord(DateTimeOffset DateTime, LogLevelRecord Level, string Thread, FileRecord File, LoggerRecord Logger, string Message, string? LogArtifacts);
