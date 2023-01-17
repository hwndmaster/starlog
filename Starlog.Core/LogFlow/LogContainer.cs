using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive;
using System.Reactive.Subjects;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.Infrastructure.Io;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core.LogFlow;

/// <summary>
///   The container of all log files for the selected profile.
/// </summary>
public interface ILogContainer : IDisposable
{
    /// <summary>
    ///   Loads a specified <paramref name="profile"/>.
    /// </summary>
    /// <param name="profile">The profile to load.</param>
    /// <returns>A task for awaiting purposes.</returns>
    Task LoadProfileAsync(Profile profile);

    /// <summary>
    ///   Returns all the files, read so far for the selected profile.
    /// </summary>
    ImmutableArray<FileRecord> GetFiles();

    /// <summary>
    ///   Returns all the logs, read so far for the selected profile.
    /// </summary>
    ImmutableArray<LogRecord> GetLogs();

    /// <summary>
    ///   Returns all unique loggers, read so far for the selected profile.
    /// </summary>
    ImmutableArray<LoggerRecord> GetLoggers();

    /// <summary>
    ///   Returns all unique log levels, read so far for the selected profile.
    /// </summary>
    ImmutableArray<LogLevelRecord> GetLogLevels();

    /// <summary>
    ///   Returns all unique threads, read so far for the selected profile.
    /// </summary>
    ImmutableArray<string> GetThreads();

    /// <summary>
    ///   An observable to handle an event when a new file is read.
    /// </summary>
    IObservable<FileRecord> FileAdded { get; }

    /// <summary>
    ///   An observable to handle an event when an existing file has been renamed.
    /// </summary>
    IObservable<(FileRecord OldRecord, FileRecord NewRecord)> FileRenamed { get; }

    /// <summary>
    ///   An observable to handle an event when a file has been removed.
    /// </summary>
    IObservable<FileRecord> FileRemoved { get; }

    /// <summary>
    ///   An observable to handle an event when a bunch of log records are read.
    /// </summary>
    IObservable<ImmutableArray<LogRecord>> LogsAdded { get; }
}

internal sealed class LogContainer : ILogContainer, ICurrentProfile
{
    private readonly IEventBus _eventBus;
    private readonly IFileService _fileService;
    private readonly ILogReaderContainer _logReaderContainer;
    private readonly ISynchronousScheduler _scheduler;
    private readonly ILogger<LogContainer> _logger;
    private readonly IFileSystemWatcher _fileWatcher;
    private readonly ConcurrentBag<LogRecord> _logs = new();
    private readonly ConcurrentBag<LoggerRecord> _loggers = new();
    private readonly ConcurrentBag<LogLevelRecord> _logLevels = new();
    private readonly ConcurrentDictionary<string, byte> _logThreads = new();
    private readonly ConcurrentDictionary<string, FileRecord> _files = new();
    private readonly Subject<Unit> _profileClosed = new();
    private readonly Subject<Profile> _profileChanged = new();
    private readonly Subject<FileRecord> _fileAdded = new();
    private readonly Subject<(FileRecord OldRecord, FileRecord NewRecord)> _fileRenamed = new();
    private readonly Subject<FileRecord> _fileRemoved = new();
    private readonly Subject<ImmutableArray<LogRecord>> _logsAdded = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public LogContainer(IEventBus eventBus, IFileService fileService,
        IFileSystemWatcher fileWatcher,
        ILogReaderContainer logReaderContainer, ISynchronousScheduler scheduler,
        ILogger<LogContainer> logger)
    {
        _eventBus = eventBus.NotNull();
        _fileService = fileService.NotNull();
        _fileWatcher = fileWatcher.NotNull();
        _logReaderContainer = logReaderContainer.NotNull();
        _scheduler = scheduler.NotNull();
        _logger = logger.NotNull();

        _fileWatcher.IncreaseBuffer();
        _fileWatcher.Created.Subscribe(FileWatcher_CreatedOrChanged);
        _fileWatcher.Changed.Subscribe(FileWatcher_CreatedOrChanged);
        _fileWatcher.Renamed.Subscribe(FileWatcher_Renamed);
        _fileWatcher.Deleted.Subscribe(FileWatcher_CreatedOrChanged);
        _fileWatcher.Error.Subscribe(FileWatcher_Error);
    }

    public async Task LoadProfileAsync(Profile profile)
    {
        _logger.LogDebug("Loading profile: {profileId}", profile.Id);

        CloseProfile();

        var isFile = false;

        if (_fileService.PathExists(profile.Path))
        {
            Profile = profile.NotNull();
            isFile = !_fileService.IsDirectory(profile.Path);

            IEnumerable<string> files;
            if (isFile)
            {
                files = new [] { profile.Path };
            }
            else
            {
                files = _fileService.EnumerateFiles(profile.Path, "*.*", new EnumerationOptions());
            }
            var tasks = files.Select(async file => await LoadFileAsync(file));
            await Task.WhenAll(tasks);

            if (!_fileWatcher.StartListening(
                path: isFile ? Path.GetDirectoryName(profile.Path).NotNull() : profile.Path,
                filter: isFile ? Path.GetFileName(profile.Path) : "*.*"))
            {
                _eventBus.Publish(new ProfileLoadingErrorEvent(profile, $"Couldn't start file monitoring over the profile path: '{profile.Path}'."));
            }

            _profileChanged.OnNext(Profile);
        }
        else
        {
            _eventBus.Publish(new ProfileLoadingErrorEvent(profile, $"Couldn't load profile since the profile path '{profile.Path}' doesn't exist."));
        }
    }

    public void CloseProfile()
    {
        _fileWatcher.StopListening();
        Profile = null;
        _files.Clear();
        _loggers.Clear();
        _logLevels.Clear();
        _logThreads.Clear();
        _logs.Clear();
        _profileClosed.OnNext(Unit.Default);
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

    public void Dispose()
    {
        _fileWatcher.Dispose();
    }

    private void FileWatcher_CreatedOrChanged(FileSystemEventArgs e)
    {
        _logger.LogDebug("File {fullPath} is {changeType}", e.FullPath, e.ChangeType);

        if (e.ChangeType == WatcherChangeTypes.Created)
        {
            _scheduler.ScheduleAsync(async () => await LoadFileAsync(e.FullPath));
        }
        else if (e.ChangeType == WatcherChangeTypes.Changed)
        {
            _scheduler.ScheduleAsync(async () =>
            {
                if (_files.TryGetValue(e.FullPath, out var fileRecord))
                {
                    using var fileStream = _fileService.OpenReadNoLock(e.FullPath);
                    fileStream.Seek(fileRecord.LastReadOffset, SeekOrigin.Begin);
                    _logger.LogDebug("File {FileName} seeking to {LastReadOffset}", fileRecord.FileName, fileRecord.LastReadOffset);
                    await ReadLogsAsync(fileStream, fileRecord).ConfigureAwait(false);
                }
                else
                {
                    _scheduler.ScheduleAsync(async () => await LoadFileAsync(e.FullPath));
                }
            });
        }
        else if (e.ChangeType == WatcherChangeTypes.Deleted)
        {
            if (_files.TryRemove(e.FullPath, out var record))
            {
                _fileRemoved.OnNext(record);
            }
        }
    }

    private void FileWatcher_Renamed(RenamedEventArgs e)
    {
        _logger.LogDebug("File {OldFullPath} is renamed to {FullPath}", e.OldFullPath, e.FullPath);

        FileRecord newRecord;
        if (!_files.TryRemove(e.OldFullPath, out var record))
            return;

        _lock.EnterWriteLock();
        try
        {
            newRecord = record.WithNewName(e.FullPath);
            _files.TryAdd(record.FileName, newRecord);

            foreach (var log in _logs.ToArray())
            {
                if (log.File == record)
                {
                    var logToRemove = log;
                    _logs.TryTake(out logToRemove);
                    _logs.Add(log with { File = newRecord });
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        _fileRenamed.OnNext((record, newRecord));
    }

    private void FileWatcher_Error(ErrorEventArgs e)
    {
        var ex = e.GetException();
        _logger.LogError(ex, ex.Message);
    }

    private async Task LoadFileAsync(string file)
    {
        if (!_fileService.FileExists(file))
            return;

        using var fileStream = _fileService.OpenReadNoLock(file);
        var fileRecord = new FileRecord(file, 0);

        await ReadLogsAsync(fileStream, fileRecord);

        _files.TryAdd(file, fileRecord);
        _fileAdded.OnNext(fileRecord);
    }

    private async Task ReadLogsAsync(Stream stream, FileRecord fileRecord)
    {
        if (Profile is null)
        {
            // No active profile.
            return;
        }

        var tp = TracePerf.Start<LogContainer>(nameof(ReadLogsAsync));

        var logReaderProcessor = _logReaderContainer.CreateLogReaderProcessor(Profile.LogReader);

        var readFileArtifacts = fileRecord.LastReadOffset == 0 && Profile.FileArtifactLinesCount > 0;
        var logRecordResult = await logReaderProcessor.ReadAsync(Profile, fileRecord, stream, readFileArtifacts);

        _logger.LogDebug("File {FileName} read {RecordsCount} logs", fileRecord.FileName, logRecordResult.Records.Length);

        fileRecord.LastReadOffset = stream.Length;

        if (readFileArtifacts)
        {
            fileRecord.Artifacts = logRecordResult.FileArtifacts;
        }

        foreach (var logger in logRecordResult.Loggers)
        {
            _loggers.Add(logger);
        }

        foreach (var logLevel in logRecordResult.LogLevels)
        {
            _logLevels.Add(logLevel);
        }

        foreach (var thread in logRecordResult.Records.Select(x => x.Thread))
        {
            _logThreads.TryAdd(thread, 0);
        }

        _lock.EnterWriteLock();
        foreach (var record in logRecordResult.Records)
        {
            _logs.Add(record);
        }
        _lock.ExitWriteLock();

        tp.StopAndReport();

        _logsAdded.OnNext(logRecordResult.Records);
    }

    public Profile? Profile { get; private set; }
    public IObservable<Unit> ProfileClosed => _profileClosed;
    public IObservable<Profile> ProfileChanged => _profileChanged;
    public IObservable<FileRecord> FileAdded => _fileAdded;
    public IObservable<(FileRecord OldRecord, FileRecord NewRecord)> FileRenamed => _fileRenamed;
    public IObservable<FileRecord> FileRemoved => _fileRemoved;
    public IObservable<ImmutableArray<LogRecord>> LogsAdded => _logsAdded;
}
