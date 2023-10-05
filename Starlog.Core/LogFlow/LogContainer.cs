using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Subjects;

namespace Genius.Starlog.Core.LogFlow;

internal class LogContainer : ILogContainer, ILogContainerWriter
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ConcurrentDictionary<string, FileRecord> _files = new();
    private readonly List<LogRecord> _logs = new();
    private readonly Subject<FileRecord> _fileAdded = new();
    private readonly ConcurrentBag<LoggerRecord> _loggers = new();
    private readonly ConcurrentBag<LogLevelRecord> _logLevels = new();
    private readonly ConcurrentDictionary<string, byte> _logThreads = new();
    private readonly Subject<(FileRecord OldRecord, FileRecord NewRecord)> _fileRenamed = new();
    private readonly Subject<FileRecord> _fileRemoved = new();
    private readonly Subject<ImmutableArray<LogRecord>> _logsAdded = new();

    public void AddFile(FileRecord fileRecord)
    {
        _files.TryAdd(fileRecord.FullPath, fileRecord);
        _fileAdded.OnNext(fileRecord);
    }

    public void AddLogger(LoggerRecord logger)
    {
        _loggers.Add(logger);
    }

    public void AddLogLevel(LogLevelRecord logLevel)
    {
        _logLevels.Add(logLevel);
    }

    public void AddThread(string thread)
    {
        _logThreads.TryAdd(thread, 0);
    }

    public void AddLogs(ImmutableArray<LogRecord> logRecords)
    {
        _lock.EnterWriteLock();
        _logs.AddRange(logRecords);
        _lock.ExitWriteLock();

        _logsAdded.OnNext(logRecords);
    }

    public ImmutableArray<FileRecord> GetFiles()
    {
        return _files.Values.ToImmutableArray();
    }

    public ImmutableArray<LogRecord> GetLogs()
    {
        _lock.EnterReadLock();
        var result = _logs.ToImmutableArray();
        _lock.ExitReadLock();

        return result;
    }

    public ImmutableArray<LoggerRecord> GetLoggers()
    {
        return _loggers.ToImmutableArray();
    }

    public ImmutableArray<LogLevelRecord> GetLogLevels()
    {
        return _logLevels.ToImmutableArray();
    }

    public ImmutableArray<string> GetThreads()
    {
        return _logThreads.Keys.ToImmutableArray();
    }

    protected void Clear()
    {
        _files.Clear();
        _loggers.Clear();
        _logLevels.Clear();
        _logThreads.Clear();
        _logs.Clear();
    }

    protected FileRecord? GetFile(string fullPath)
    {
        return _files.TryGetValue(fullPath, out var fileRecord) ? fileRecord : null;
    }

    protected FileRecord? RemoveFile(string fullPath, bool triggerEvent = true)
    {
        if (_files.TryRemove(fullPath, out var record))
        {
            if (triggerEvent)
            {
                _fileRemoved.OnNext(record);
            }
            return record;
        }

        return null;
    }

    protected void RenameFile(FileRecord previousRecord, string fullPath)
    {
        FileRecord newRecord;
        _lock.EnterWriteLock();
        try
        {
            newRecord = previousRecord.WithNewName(fullPath);
            _files.TryAdd(previousRecord.FileName, newRecord);

            for (var i = 0; i < _logs.Count; i++)
            {
                if (_logs[i].File == previousRecord)
                {
                    _logs[i] = _logs[i] with { File = newRecord };
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        _fileRenamed.OnNext((previousRecord, newRecord));
    }

    public IObservable<FileRecord> FileAdded => _fileAdded;
    public IObservable<(FileRecord OldRecord, FileRecord NewRecord)> FileRenamed => _fileRenamed;
    public IObservable<FileRecord> FileRemoved => _fileRemoved;
    public IObservable<ImmutableArray<LogRecord>> LogsAdded => _logsAdded;
}
