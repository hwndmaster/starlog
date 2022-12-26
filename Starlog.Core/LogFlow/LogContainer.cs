using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive;
using System.Reactive.Subjects;
using Genius.Atom.Infrastructure.Io;
using Genius.Starlog.Core.LogReading;
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
    ///   Closes down currently loaded profile.
    /// </summary>
    void CloseProfile();

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
    ///   An observable to handle an event when a bunch of log records are read.
    /// </summary>
    IObservable<ImmutableArray<LogRecord>> LogsAdded { get; }
}

internal sealed class LogContainer : ILogContainer, ICurrentProfile
{
    private readonly IFileService _fileService;
    private readonly ILogReaderContainer _logReaderContainer;
    private readonly FileSystemWatcher _fileWatcher;
    private readonly ISynchronousScheduler _scheduler;
    private readonly ILogger<LogContainer> _logger;
    private readonly ConcurrentBag<LogRecord> _logs = new();
    private readonly ConcurrentBag<LoggerRecord> _loggers = new();
    private readonly ConcurrentBag<LogLevelRecord> _logLevels = new();
    private readonly ConcurrentDictionary<string, byte> _logThreads = new();
    private readonly ConcurrentDictionary<string, FileRecord> _files = new();
    private readonly Subject<Unit> _profileChanging = new();
    private readonly Subject<Profile> _profileChanged = new();
    private readonly Subject<FileRecord> _fileAdded = new();
    private readonly Subject<ImmutableArray<LogRecord>> _logsAdded = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public LogContainer(IFileService fileService, ILogReaderContainer logReaderContainer, ISynchronousScheduler scheduler,
        ILogger<LogContainer> logger)
    {
        _fileService = fileService.NotNull();
        _logReaderContainer = logReaderContainer.NotNull();
        _scheduler = scheduler.NotNull();
        _logger = logger.NotNull();

        _fileWatcher = new FileSystemWatcher
        {
            EnableRaisingEvents = false,
            Filter = "*.*",
        };
        _fileWatcher.Created += FileWatcher_CreatedOrChanged;
        _fileWatcher.Changed += FileWatcher_CreatedOrChanged;
        _fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
    }

    public async Task LoadProfileAsync(Profile profile)
    {
        _logger.LogDebug("Loading profile: {profileId}", profile.Id);

        _profileChanging.OnNext(Unit.Default);

        CloseProfile();

        Profile = profile.NotNull();

        var isFile = false;

        if (Path.Exists(profile.Path))
        {
            var pathAttr = File.GetAttributes(profile.Path);
            IEnumerable<string> files;
            if (pathAttr.HasFlag(FileAttributes.Directory))
            {
                files = _fileService.EnumerateFiles(profile.Path, "*.*", new EnumerationOptions());
            }
            else
            {
                files = new [] { profile.Path };
                isFile = true;
            }
            var tasks = files.Select(async file => await LoadFileAsync(file));
            await Task.WhenAll(tasks);

            _fileWatcher.Path = isFile ? Path.GetDirectoryName(profile.Path).NotNull() : profile.Path;
            _fileWatcher.Filter = isFile ? Path.GetFileName(profile.Path) : "*.*";
            _fileWatcher.EnableRaisingEvents = true;
        }

        _profileChanged.OnNext(Profile);
    }

    public void CloseProfile()
    {
        _fileWatcher.EnableRaisingEvents = false;
        _files.Clear();
        _loggers.Clear();
        _logLevels.Clear();
        _logThreads.Clear();
        _logs.Clear();
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

    private void FileWatcher_CreatedOrChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("File {fullPath} is {changeType}", e.FullPath, e.ChangeType);

        if (e.ChangeType == WatcherChangeTypes.Created)
        {
            _scheduler.ScheduleAsync(async () => await LoadFileAsync(e.FullPath));
        }
        else if (e.ChangeType == WatcherChangeTypes.Changed)
        {
            if (_files.TryGetValue(e.FullPath, out var fileRecord))
            {
                _scheduler.ScheduleAsync(async () =>
                {
                    using Stream fileStream = _fileService.OpenRead(e.FullPath);
                    fileStream.Seek(fileRecord.LastReadOffset, SeekOrigin.Begin);

                    fileRecord.LastReadOffset = fileStream.Length;

                    await ReadLogsAsync(fileStream, fileRecord).ConfigureAwait(false);
                });
            }
            else
            {
                _scheduler.ScheduleAsync(async () => await LoadFileAsync(e.FullPath));
            }
        }
    }

    private async Task LoadFileAsync(string file)
    {
        var fileName = Path.GetFileName(file);
        using var fileStream = _fileService.OpenRead(file);
        var fileRecord = new FileRecord(file, fileName, 0);

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
    public IObservable<Unit> ProfileChanging => _profileChanging;
    public IObservable<Profile> ProfileChanged => _profileChanged;
    public IObservable<FileRecord> FileAdded => _fileAdded;
    public IObservable<ImmutableArray<LogRecord>> LogsAdded => _logsAdded;
}
