using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Subjects;
using Genius.Atom.Infrastructure.Io;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

public interface ILogContainer : IDisposable
{
    Task LoadProfileAsync(Profile profile);
    ImmutableArray<LogRecord> GetLogs();
}

internal sealed class LogContainer : ILogContainer
{
    private readonly IFileService _fileService;
    private readonly ILogReaderContainer _logReaderContainer;
    private readonly FileSystemWatcher _fileWatcher;
    private readonly ISynchronousScheduler _scheduler;
    private readonly ConcurrentBag<LogRecord> _logs = new();
    private readonly ConcurrentDictionary<string, FileRecord> _files = new();
    private readonly Subject<ImmutableArray<LogRecord>> _logsAdded = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public LogContainer(IFileService fileService, ILogReaderContainer logReaderContainer, ISynchronousScheduler scheduler)
    {
        _fileService = fileService.NotNull();
        _logReaderContainer = logReaderContainer.NotNull();
        _scheduler = scheduler.NotNull();

        _fileWatcher = new FileSystemWatcher
        {
            EnableRaisingEvents = false,
            Filter = "*.*",
        };
        _fileWatcher.Created += FileWatcher_CreatedOrChanged;
        _fileWatcher.Changed += FileWatcher_CreatedOrChanged;
        _fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
    }

    public Task LoadProfileAsync(Profile profile)
    {
        _fileWatcher.EnableRaisingEvents = false;

        Profile = profile.NotNull();
        _logs.Clear();

        var files = _fileService.EnumerateFiles(profile.Path, "*.*", new EnumerationOptions());
        Parallel.ForEach(files, async (file, lps, i) =>
            await LoadFileAsync(file));

        _fileWatcher.Path = profile.Path;
        _fileWatcher.EnableRaisingEvents = true;

        return Task.CompletedTask;
    }

    public ImmutableArray<LogRecord> GetLogs()
    {
        _lock.EnterReadLock();
        var result = _logs.ToImmutableArray();
        _lock.ExitReadLock();

        return result;
    }

    public void Dispose()
    {
        _fileWatcher.Dispose();
    }

    private void FileWatcher_CreatedOrChanged(object sender, FileSystemEventArgs e)
    {
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
        var fileRecord = new FileRecord(file, fileName, fileStream.Length);

        await ReadLogsAsync(fileStream, fileRecord);

        _files.TryAdd(file, fileRecord);
    }

    private async Task ReadLogsAsync(Stream stream, FileRecord fileRecord)
    {
        if (Profile is null)
        {
            // No active profile.
            return;
        }

        var logReaderProcessor = _logReaderContainer.CreateLogReaderProcessor(Profile.LogReader);
        var logRecords = (await logReaderProcessor.ReadAsync(Profile, fileRecord, stream)).ToImmutableArray();

        _lock.EnterWriteLock();
        foreach (var record in logRecords)
        {
            _logs.Add(record);
        }
        _lock.ExitWriteLock();

        _logsAdded.OnNext(logRecords);
    }

    public Profile? Profile { get; private set; }
    public IObservable<ImmutableArray<LogRecord>> LogsAdded => _logsAdded;
}
