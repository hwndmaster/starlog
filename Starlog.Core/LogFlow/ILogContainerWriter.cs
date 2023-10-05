using System.Collections.Immutable;

namespace Genius.Starlog.Core.LogFlow;

internal interface ILogContainerWriter : ILogContainer
{
    void AddFile(FileRecord fileRecord);
    void AddLogger(LoggerRecord logger);
    void AddLogLevel(LogLevelRecord logLevel);
    void AddLogs(ImmutableArray<LogRecord> logRecords);
    void AddThread(string thread);
}
