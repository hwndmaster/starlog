using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

/// <summary>
///   A record contains information regarding a log level.
/// </summary>
public readonly record struct LogLevelRecord(int Id, string Name, LogSeverity Severity);
