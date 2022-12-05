using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

public readonly record struct LogLevelRecord(int Id, string Name, LogSeverity Severity);
