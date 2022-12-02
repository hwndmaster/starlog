namespace Genius.Starlog.Core.LogFlow;

public readonly record struct LogRecord(DateTimeOffset DateTime, LogLevelRecord Level, string Thread, FileRecord File, FileArtifacts FileArtifacts, LoggerRecord Logger, string Message, string? LogArtifacts);
