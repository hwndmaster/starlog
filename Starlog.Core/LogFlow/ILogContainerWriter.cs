using System.Collections.Immutable;

namespace Genius.Starlog.Core.LogFlow;

internal interface ILogContainerWriter : ILogContainer
{
    void AddSource(LogSourceBase source);
    void AddLogger(LoggerRecord logger);
    void AddLogLevel(LogLevelRecord logLevel);
    void AddLogs(ImmutableArray<LogRecord> logRecords);
    void AddThread(string thread);
    void RemoveSource(string name);
    void RenameSource(string oldName, string newName);
}
