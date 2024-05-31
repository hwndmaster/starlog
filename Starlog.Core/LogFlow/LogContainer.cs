using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Genius.Atom.Infrastructure.Threading;

namespace Genius.Starlog.Core.LogFlow;

internal class LogContainer : ILogContainerWriter, IDisposable
{
    protected readonly Disposer _disposer = new();
    private readonly ConcurrentDictionary<string, LogSourceBase> _sources = new();
    private readonly List<LogRecord> _logs = [];
    private readonly ConcurrentBag<LogLevelRecord> _logLevels = [];
    private readonly LogFieldsContainer _fields = new();

#pragma warning disable CA2213 // Disposable fields should be disposed
    // The following objects are being already added to `_disposer`:
    private readonly ReaderWriterLockSlim _logsAccessingLock;
    private readonly Subject<LogSourceBase> _sourceAdded;
    private readonly Subject<(LogSourceBase OldRecord, LogSourceBase NewRecord)> _sourceRenamed;
    private readonly Subject<LogSourceBase> _sourceRemoved;
    private readonly Subject<int> _sourcesCountChanged;
    private readonly Subject<ImmutableArray<LogRecord>> _logsAdded;
    private readonly Subject<ImmutableArray<LogRecord>> _logsRemoved;
#pragma warning restore CA2213 // Disposable fields should be disposed

    public LogContainer()
    {
        _logsAccessingLock = _disposer.Add(new ReaderWriterLockSlim());
        _logsAdded = _disposer.Add(new Subject<ImmutableArray<LogRecord>>());
        _logsRemoved = _disposer.Add(new Subject<ImmutableArray<LogRecord>>());
        _sourceAdded = _disposer.Add(new Subject<LogSourceBase>());
        _sourceRemoved = _disposer.Add(new Subject<LogSourceBase>());
        _sourceRenamed = _disposer.Add(new Subject<(LogSourceBase OldRecord, LogSourceBase NewRecord)>());
        _sourcesCountChanged = _disposer.Add(new Subject<int>());

        _sourceAdded
            .Concat(_sourceRemoved)
            .Subscribe(_ => _sourcesCountChanged.OnNext(SourcesCount))
            .DisposeWith(_disposer);
    }

    public void AddSource(LogSourceBase source)
    {
        _sources.TryAdd(source.Name, source);
        _sourceAdded.OnNext(source);
    }

    public void AddLogLevel(LogLevelRecord logLevel)
    {
        _logLevels.Add(logLevel);
    }

    public void AddLogs(ImmutableArray<LogRecord> logRecords)
    {
        using (_logsAccessingLock.BeginWriteLock())
        {
            _logs.AddRange(logRecords);
        }

        _logsAdded.OnNext(logRecords);
    }

    public ImmutableArray<LogSourceBase> GetSources()
        => _sources.Values.ToImmutableArray();

    public ImmutableArray<LogRecord> GetLogs()
    {
        _logsAccessingLock.EnterReadLock();
        var result = _logs.ToImmutableArray();
        _logsAccessingLock.ExitReadLock();

        return result;
    }

    public ILogFieldsContainerReadonly GetFields()
        => _fields;
    public ILogFieldsContainer GetFieldsContainer()
        => _fields;
    public ImmutableArray<LogLevelRecord> GetLogLevels()
        => _logLevels.ToImmutableArray();
    public LogSourceBase? GetSource(string name)
        => _sources.TryGetValue(name, out var source) ? source : null;

    public void RemoveSource(string name)
    {
        if (_sources.TryRemove(name, out var record))
        {
            List<LogRecord> logsRemoved = [];
            List<HashSet<int>> fieldValuesAffected = [];

            while (fieldValuesAffected.Count < _fields.GetFieldCount() + 1)
                fieldValuesAffected.Add([]);

            // Step 1: Remove logs attached to the removing source
            _logs.RemoveAll(x =>
            {
                if (x.Source.Name.Equals(record.Name, StringComparison.InvariantCulture))
                {
                    logsRemoved.Add(x);
                    for (var i = 0; i < x.FieldValueIndices.Length; i++)
                        fieldValuesAffected[i].Add(x.FieldValueIndices[i]);
                    return true;
                }
                return false;
            });

            // Step 2: Check field values from remaining log items
            HashSet<int>[] remainingFieldValues = new HashSet<int>[_fields.GetFieldCount()];
            for (var i = 0; i < remainingFieldValues.Length; i++)
                remainingFieldValues[i] = [];
            for (var logIndex = 0; logIndex < _logs.Count; logIndex++)
                for (var fieldIndex = 0; fieldIndex < _logs[logIndex].FieldValueIndices.Length; fieldIndex++)
                    remainingFieldValues[fieldIndex].Add(_logs[logIndex].FieldValueIndices[fieldIndex]);

            // Step 3: Check the validity of the affected sub records
            for (var fieldId = 0; fieldId < remainingFieldValues.Length; fieldId++)
            {
                for (var fieldValueIndex = 0; fieldValueIndex < fieldValuesAffected[fieldId].Count; fieldValueIndex++)
                {
                    var fieldValueId = fieldValuesAffected[fieldId].ElementAt(fieldValueIndex);
                    if (!remainingFieldValues[fieldId].Contains(fieldValueId))
                    {
                        _fields.RemoveFieldValue(fieldId, fieldValueId);
                    }
                }
            }

            // Step 4: Raise events
            _logsRemoved.OnNext(logsRemoved.ToImmutableArray());
            _sourceRemoved.OnNext(record);
        }
    }

    public void RenameSource(string oldName, string newName)
    {
        LogSourceBase newSource;
        LogSourceBase? previousSource;

        using (_logsAccessingLock.BeginWriteLock())
        {
            if (!_sources.TryRemove(oldName, out previousSource))
            {
                return;
            }

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

        _sourceRenamed.OnNext((previousSource, newSource));
    }

    public void Dispose()
    {
        _disposer.Dispose();
    }

    protected void Clear()
    {
        _fields.Clear();
        _sources.Clear();
        _logLevels.Clear();
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
