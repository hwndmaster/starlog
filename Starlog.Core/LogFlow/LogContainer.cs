using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Genius.Starlog.Core.LogFlow;

internal class LogContainer : ILogContainer, ILogContainerWriter
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ConcurrentDictionary<string, LogSourceBase> _sources = new();
    private readonly List<LogRecord> _logs = new();
    private readonly ConcurrentDictionary<int, LoggerRecord> _loggers = new();
    private readonly ConcurrentBag<LogLevelRecord> _logLevels = new();
    private readonly ConcurrentDictionary<string, byte> _logThreads = new();

    private readonly Subject<LogSourceBase> _sourceAdded = new();
    private readonly Subject<(LogSourceBase OldRecord, LogSourceBase NewRecord)> _sourceRenamed = new();
    private readonly Subject<LogSourceBase> _sourceRemoved = new();
    private readonly Subject<int> _sourcesCountChanged = new();
    private readonly Subject<ImmutableArray<LogRecord>> _logsAdded = new();
    private readonly Subject<ImmutableArray<LogRecord>> _logsRemoved = new();

    public LogContainer()
    {
        // TODO: Cover with unit tests
        _sourceAdded
            .Concat(_sourceRemoved)
            .Subscribe(_ => _sourcesCountChanged.OnNext(SourcesCount));
    }

    public void AddSource(LogSourceBase sourceRecord)
    {
        _sources.TryAdd(sourceRecord.Name, sourceRecord);
        _sourceAdded.OnNext(sourceRecord);
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

    public ImmutableArray<LogSourceBase> GetSources()
    {
        return _sources.Values.ToImmutableArray();
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

    public LogSourceBase? GetSource(string name)
    {
        return _sources.TryGetValue(name, out var source) ? source : null;
    }

    // TODO: Cover with unit tests
    public void RemoveSource(string name)
    {
        if (_sources.TryRemove(name, out var record))
        {
            List<LogRecord> logsRemoved = new();
            HashSet<int> loggersAffected = new();
            HashSet<string> logThreadsAffected = new();

            // Step 1: Remove logs
            _logs.RemoveAll(x =>
            {
                if (x.Source.Name.Equals(record.Name, StringComparison.InvariantCulture))
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
            _sourceRemoved.OnNext(record);
        }
    }

    public void RenameSource(string oldName, string newName)
    {
        LogSourceBase newSource;
        _lock.EnterWriteLock();

        if (!_sources.TryRemove(oldName, out var previousSource))
        {
            return;
        }

        try
        {
            newSource = previousSource.WithNewName(newName);
            _sources.TryAdd(previousSource.Name, newSource);

            for (var i = 0; i < _logs.Count; i++)
            {
                if (_logs[i].Source == previousSource)
                {
                    _logs[i] = _logs[i] with { Source = newSource };
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        _sourceRenamed.OnNext((previousSource, newSource));
    }

    protected void Clear()
    {
        _sources.Clear();
        _loggers.Clear();
        _logLevels.Clear();
        _logThreads.Clear();
        _logs.Clear();
    }

    public IObservable<LogSourceBase> SourceAdded => _sourceAdded;
    public IObservable<(LogSourceBase OldRecord, LogSourceBase NewRecord)> SourceRenamed => _sourceRenamed;
    public IObservable<LogSourceBase> SourceRemoved => _sourceRemoved;
    public IObservable<int> SourcesCountChanged => _sourcesCountChanged;
    public IObservable<ImmutableArray<LogRecord>> LogsAdded => _logsAdded;
    public IObservable<ImmutableArray<LogRecord>> LogsRemoved => _logsRemoved;

    public int SourcesCount => _sources.Count;
}
