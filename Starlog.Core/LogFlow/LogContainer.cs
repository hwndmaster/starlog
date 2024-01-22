using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Genius.Starlog.Core.LogFlow;

internal class LogContainer : ILogContainer, ILogContainerWriter
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ConcurrentDictionary<string, FileRecord> _files = new();
    private readonly List<LogRecord> _logs = new();
    private readonly ConcurrentDictionary<int, LoggerRecord> _loggers = new();
    private readonly ConcurrentBag<LogLevelRecord> _logLevels = new();
    private readonly ConcurrentDictionary<string, byte> _logThreads = new();

    private readonly Subject<FileRecord> _fileAdded = new();
    private readonly Subject<(FileRecord OldRecord, FileRecord NewRecord)> _fileRenamed = new();
    private readonly Subject<FileRecord> _fileRemoved = new();
    private readonly Subject<int> _filesCountChanged = new();
    private readonly Subject<ImmutableArray<LogRecord>> _logsAdded = new();
    private readonly Subject<ImmutableArray<LogRecord>> _logsRemoved = new();

    public LogContainer()
    {
        // TODO: Cover with unit tests
        _fileAdded
            .Concat(_fileRemoved)
            .Subscribe(_ => _filesCountChanged.OnNext(FilesCount));
    }

    public void AddFile(FileRecord fileRecord)
    {
        _files.TryAdd(fileRecord.FullPath, fileRecord);
        _fileAdded.OnNext(fileRecord);
    }

    public void AddLogger(LoggerRecord logger)
    {
        _loggers.TryAdd(logger.Id, logger);
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
        return _loggers.Values.ToImmutableArray();
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

    // TODO: Cover with unit tests
    protected void RemoveFile(string fullPath)
    {
        if (_files.TryRemove(fullPath, out var record))
        {
            List<LogRecord> logsRemoved = new();
            HashSet<int> loggersAffected = new();
            HashSet<string> logThreadsAffected = new();

            // Step 1: Remove logs
            _logs.RemoveAll(x =>
            {
                if (x.File.FullPath.Equals(record.FullPath, StringComparison.InvariantCulture))
                {
                    logsRemoved.Add(x);
                    loggersAffected.Add(x.Logger.Id);
                    logThreadsAffected.Add(x.Thread);
                    return true;
                }
                return false;
            });

            // Step 2: Check the validity of the affected sub records
            var loggersLookup = _logs.ToLookup(x => x.Logger.Id);
            foreach (var loggerId in loggersAffected)
            {
                if (!loggersLookup[loggerId].Any())
                {
                    _loggers.TryRemove(loggerId, out var _);
                }
            }
            var threadsLookup = _logs.ToLookup(x => x.Thread);
            foreach (var thread in logThreadsAffected)
            {
                if (!threadsLookup[thread].Any())
                {
                    _logThreads.TryRemove(thread, out var _);
                }
            }

            // Step 3: Raise events
            _logsRemoved.OnNext(logsRemoved.ToImmutableArray());
            _fileRemoved.OnNext(record);
        }
    }

    protected void RenameFile(string oldFullPath, string newFullPath)
    {
        FileRecord newRecord;
        _lock.EnterWriteLock();

        if (!_files.TryRemove(oldFullPath, out var previousRecord))
        {
            return;
        }

        try
        {
            newRecord = previousRecord.WithNewName(newFullPath);
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
    public IObservable<int> FilesCountChanged => _filesCountChanged;
    public IObservable<ImmutableArray<LogRecord>> LogsAdded => _logsAdded;
    public IObservable<ImmutableArray<LogRecord>> LogsRemoved => _logsRemoved;

    public int FilesCount => _files.Count;
}
