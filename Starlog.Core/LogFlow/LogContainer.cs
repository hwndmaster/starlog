using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Genius.Atom.Infrastructure.Threading;

namespace Genius.Starlog.Core.LogFlow;

internal class LogContainer : ILogContainer, ILogContainerWriter, IDisposable
{
    private readonly ReaderWriterLockSlim _logsAccessingLock = new();
    private readonly ConcurrentDictionary<string, LogSourceBase> _sources = new();
    private readonly List<LogRecord> _logs = [];
    private readonly ConcurrentBag<LogLevelRecord> _logLevels = [];
    private readonly LogFieldsContainer _fields = new();

    private readonly Subject<LogSourceBase> _sourceAdded = new();
    private readonly Subject<(LogSourceBase OldRecord, LogSourceBase NewRecord)> _sourceRenamed = new();
    private readonly Subject<LogSourceBase> _sourceRemoved = new();
    private readonly Subject<int> _sourcesCountChanged = new();
    private readonly Subject<ImmutableArray<LogRecord>> _logsAdded = new();
    private readonly Subject<ImmutableArray<LogRecord>> _logsRemoved = new();

    public LogContainer()
    {
        _sourceAdded
            .Concat(_sourceRemoved)
            .Subscribe(_ => _sourcesCountChanged.OnNext(SourcesCount));
    }

    public void AddSource(LogSourceBase sourceRecord)
    {
        _sources.TryAdd(sourceRecord.Name, sourceRecord);
        _sourceAdded.OnNext(sourceRecord);
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
                _logsAccessingLock.ExitWriteLock();
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
                _logsAccessingLock.ExitWriteLock();
            }
        }

        _sourceRenamed.OnNext((previousSource, newSource));
    }

    public void Dispose()
    {
        _logsAccessingLock.Dispose();
        _logsAdded.Dispose();
        _logsRemoved.Dispose();
        _sourceAdded.Dispose();
        _sourceRemoved.Dispose();
        _sourceRenamed.Dispose();
        _sourcesCountChanged.Dispose();
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
