using System.Collections.Immutable;
using System.Reactive;
using System.Reactive.Subjects;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.Infrastructure.Io;
using Genius.Atom.Infrastructure.Tasks;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core.ProfileLoading;

internal sealed class FileBasedProfileLoader : IProfileLoader
{
    internal const int UPDATE_LASTREADSIZE_WAIT_TIMEOUT_MS = 200;

    private readonly IDirectoryMonitor _directoryMonitor;
    private readonly IEventBus _eventBus;
    private readonly IFileService _fileService;
    private readonly IFileSystemWatcherFactory _fileSystemWatcherFactory;
    private readonly ILogCodecContainerInternal _logCodecContainer;
    private readonly ISynchronousScheduler _scheduler;
    private readonly ILogger<FileBasedProfileLoader> _logger;

    public FileBasedProfileLoader(
        IDirectoryMonitor directoryMonitor,
        IEventBus eventBus,
        IFileService fileService,
        IFileSystemWatcherFactory fileSystemWatcherFactory,
        ILogCodecContainerInternal logCodecContainer,
        ILogger<FileBasedProfileLoader> logger,
        ISynchronousScheduler scheduler)
    {
        _directoryMonitor = directoryMonitor.NotNull();
        _eventBus = eventBus.NotNull();
        _fileService = fileService.NotNull();
        _fileSystemWatcherFactory = fileSystemWatcherFactory.NotNull();
        _logCodecContainer = logCodecContainer.NotNull();
        _logger = logger.NotNull();
        _scheduler = scheduler.NotNull();
    }

    public async Task<ProfileStateBase> LoadProfileAsync(Profile profile, ILogContainerWriter logContainer)
    {
        Guard.NotNull(profile);
        Guard.NotNull(logContainer);

        _logger.LogDebug("Loading profile: {ProfileId}", profile.Id);

        if (profile.Settings is not IFileBasedProfileSettings fileBasedProfileSettings)
            throw new InvalidOperationException("Profile is not file based as expected in this routine");

        var nonExistingPaths = fileBasedProfileSettings.Paths.Where(x => !_fileService.PathExists(x)).ToArray();
        if (nonExistingPaths.Length > 0)
        {
            if (nonExistingPaths.Length == fileBasedProfileSettings.Paths.Length)
            {
                throw new InvalidOperationException($"None of the paths ({string.Join(", ", nonExistingPaths)}) were found.");
            }
            else
            {
                _eventBus.Publish(new ProfileLoadingErrorEvent(profile, "The following paths were ignored since they do not exist: " + string.Join(", ", nonExistingPaths)));
            }
        }

        var existingPaths = fileBasedProfileSettings.Paths.Except(nonExistingPaths);
        var files = existingPaths.SelectMany(x =>
            {
                if (_fileService.IsDirectory(x))
                {
                    return _fileService.EnumerateFiles(x, fileBasedProfileSettings.LogsLookupPattern, new EnumerationOptions());
                }
                else
                {
                    return [x];
                }
            });
        var tasks = files.Select(async file => await LoadFileAsync(profile, file, logContainer));
        await Task.WhenAll(tasks);

        var pathsToWatch = (from path in existingPaths
                            let isFileBasedProfile = !_fileService.IsDirectory(path)
                            let pathTrim = path.TrimEnd('\\', '/')
                            let watchPath = isFileBasedProfile ? Path.GetDirectoryName(pathTrim).NotNull() : pathTrim
                            select (watchPath, isFileBasedProfile, pathTrim))
                           .GroupBy(x => (x.watchPath, x.isFileBasedProfile), x => x.pathTrim);

        return new FilesBasedProfileState
        {
            Profile = profile,
            Settings = fileBasedProfileSettings,
            WatchingDirectories = pathsToWatch.Select(x =>
                new FilesBasedProfileState.WatchingDirectory(x.Key.watchPath, x.Key.isFileBasedProfile, x.Key.isFileBasedProfile ? x.ToImmutableArray() : []))
                .ToImmutableArray(),
        };
    }

    public IDisposable StartProfileMonitoring(ProfileStateBase profileState, ILogContainerWriter logContainer, Subject<Unit> unknownChangesDetectedSubject)
    {
        if (profileState is not FilesBasedProfileState filesBasedProfileState)
            throw new InvalidOperationException("Cannot start profile monitoring, since it is not file based.");

        var disposer = new Disposer();

        foreach (var pathToWatch in filesBasedProfileState.WatchingDirectories)
        {
            var watchFilter = pathToWatch switch
            {
                { IsFileBased: true, ForFiles.Length: 1 } => Path.GetFileName(pathToWatch.ForFiles[0]),
                { IsFileBased: true, ForFiles.Length: >1 } => filesBasedProfileState.Settings.LogsLookupPattern,
                { IsFileBased: false } => filesBasedProfileState.Settings.LogsLookupPattern,
                _ => null
            };
            if (watchFilter is null)
            {
                _logger.LogError("Could not initialize a watchFilter. Path = '{Path}', ForFiles.Length = {ForFilesLen}", pathToWatch.Path, pathToWatch.ForFiles.Length);
                continue;
            }
            var fileWatcher = (_fileSystemWatcherFactory.Create(pathToWatch.Path, watchFilter, increaseBuffer: true)
                ?? throw new InvalidOperationException("Couldn't run file watcher for the path: " + pathToWatch.Path))
                .DisposeWith(disposer);

            fileWatcher.Created.Subscribe(args => FileWatcher_CreatedOrChangedOrDeleted(filesBasedProfileState, logContainer, args)).DisposeWith(disposer);
            fileWatcher.Changed.Subscribe(args => FileWatcher_CreatedOrChangedOrDeleted(filesBasedProfileState, logContainer, args)).DisposeWith(disposer);
            fileWatcher.Renamed.Subscribe(args => FileWatcher_Renamed(logContainer, args)).DisposeWith(disposer);
            fileWatcher.Deleted.Subscribe(args => FileWatcher_CreatedOrChangedOrDeleted(filesBasedProfileState, logContainer, args)).DisposeWith(disposer);
            fileWatcher.Error.Subscribe(FileWatcher_Error).DisposeWith(disposer);

            if (!pathToWatch.IsFileBased)
            {
                // Currently DirectoryMonitor supports only one folder to monitor.
                // Since recent, a profile may have multiple folders included.
                // For now only the last directory be monitored.
                _directoryMonitor.StartMonitoring(pathToWatch.Path, filesBasedProfileState.Settings.LogsLookupPattern)
                    .DisposeWith(disposer);
                _directoryMonitor.Pulse.Subscribe(size =>
                    DirectoryMonitor_PulseAsync(filesBasedProfileState, size, unknownChangesDetectedSubject).RunAndForget())
                    .DisposeWith(disposer);
            }
        }

        UpdateLastReadSize(filesBasedProfileState);

        return disposer;
    }

    private async Task<bool> LoadFileAsync(Profile profile, string file, ILogContainerWriter logContainer)
    {
        if (!_fileService.FileExists(file))
        {
            return false;
        }

        Guard.NotNull(profile);
        Guard.NotNull(logContainer);

        using var fileStream = _fileService.OpenReadNoLock(file);
        var fileRecord = new FileRecord(file, 0);

        await ReadLogsAsync(profile, fileStream, fileRecord, logContainer);

        logContainer.AddSource(fileRecord);

        return true;
    }

    private async Task<bool> ReadLogsAsync(Profile profile, Stream stream, FileRecord fileRecord, ILogContainerWriter logContainer)
    {
        Guard.NotNull(profile);
        Guard.NotNull(stream);
        Guard.NotNull(fileRecord);
        Guard.NotNull(logContainer);

        if (profile.Settings is not PlainTextProfileSettings)
            throw new InvalidOperationException("Cannot read profile logs, since it is not plain text.");

        var tp = TracePerf.Start<FileBasedProfileLoader>(nameof(ReadLogsAsync));

        var logCodecProcessor = _logCodecContainer.FindLogCodecProcessor(profile.Settings);

        var settings = new LogReadingSettings(
            ReadSourceArtifacts: fileRecord.LastReadOffset == 0 && logCodecProcessor.MayContainSourceArtifacts(profile.Settings)
        );
        var logRecordResult = await logCodecProcessor.ReadAsync(profile, fileRecord, stream, settings, logContainer.GetFieldsContainer());

        _logger.LogDebug("File {FileName} read {RecordsCount} logs", fileRecord.FileName, logRecordResult.Records.Length);

        if (logRecordResult.Errors.Count > 0)
        {
            var reason = string.Join(Environment.NewLine, logRecordResult.Errors);
            _eventBus.Publish(new ProfileLoadingErrorEvent(profile, reason));
            _logger.LogDebug("File {FileName} found {RecordsCount} errors\r\n{Reason}", fileRecord.FileName, logRecordResult.Errors.Count, reason);
        }

        fileRecord.LastReadOffset = stream.Length;

        if (settings.ReadSourceArtifacts)
        {
            fileRecord.Artifacts = logRecordResult.FileArtifacts;
        }

        foreach (var logLevel in logRecordResult.LogLevels)
        {
            logContainer.AddLogLevel(logLevel);
        }

        logContainer.AddLogs(logRecordResult.Records);

        tp.StopAndReport();

        return true;
    }

    private static async Task DirectoryMonitor_PulseAsync(FilesBasedProfileState profileState, long profileDirectorySize, Subject<Unit> unknownChangesDetectedSubject)
    {
        if (profileState.LastReadSize != profileDirectorySize)
        {
            // Wait a little longer to ensure `UpdateLastReadSize()` has finished its work
            await Task.Delay(UPDATE_LASTREADSIZE_WAIT_TIMEOUT_MS).ConfigureAwait(false);

            if (profileState.LastReadSize != profileDirectorySize)
            {
                lock (profileState)
                {
                    profileState.LastReadSize = profileDirectorySize;
                }
                unknownChangesDetectedSubject.OnNext(Unit.Default);
            }
        }
    }

    private void FileWatcher_CreatedOrChangedOrDeleted(FilesBasedProfileState profileState, ILogContainerWriter logContainer, FileSystemEventArgs e)
    {
        _logger.LogDebug("File {FullPath} was {ChangeType}", e.FullPath, e.ChangeType);

        UpdateLastReadSize(profileState);

        // TODO: Cover the logic change with unit tests
        // The handling will take place in one of the cases:
        // 1) If the operating file belongs to a profile's directory
        // 2) If the operating file is one of the profile's listed path.
        var operatingDirectory = _fileService.IsDirectory(e.FullPath) ? e.FullPath : Path.GetDirectoryName(e.FullPath).NotNull();
        if (!profileState.WatchingDirectories.Any(
            x => (!x.IsFileBased && operatingDirectory.Equals(x.Path)) // 1
            || (x.IsFileBased && x.ForFiles.Any(ff => ff.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase))) // 2
        ))
        {
            return;
        }

        if (e.ChangeType == WatcherChangeTypes.Created)
        {
            _scheduler.Schedule(async () => await LoadFileAsync(profileState.Profile, e.FullPath, logContainer));
        }
        else if (e.ChangeType == WatcherChangeTypes.Changed)
        {
            _scheduler.Schedule(async () =>
            {
                var source = logContainer.GetSource(e.FullPath);
                if (source is FileRecord fileRecord)
                {
                    using var fileStream = _fileService.OpenReadNoLock(e.FullPath);
                    fileStream.Seek(fileRecord.LastReadOffset, SeekOrigin.Begin);
                    _logger.LogDebug("File {FileName} seeking to {LastReadOffset}", fileRecord.FileName, fileRecord.LastReadOffset);
                    await ReadLogsAsync(profileState.Profile, fileStream, fileRecord, logContainer).ConfigureAwait(false);
                }
                else
                {
                    _scheduler.Schedule(async () => await LoadFileAsync(profileState.Profile, e.FullPath, logContainer));
                }
            });
        }
        else if (e.ChangeType == WatcherChangeTypes.Deleted)
        {
            logContainer.RemoveSource(e.FullPath);
        }
    }

    private void FileWatcher_Renamed(ILogContainerWriter logContainer, RenamedEventArgs e)
    {
        _logger.LogDebug("File {OldFullPath} was renamed to {FullPath}", e.OldFullPath, e.FullPath);

        logContainer.RenameSource(e.OldFullPath, e.FullPath);
    }

    private void FileWatcher_Error(ErrorEventArgs e)
    {
        var ex = e.GetException();
        _logger.LogError(ex, ex.Message);
    }

    private void UpdateLastReadSize(FilesBasedProfileState profileState)
    {
        var lastReadSize = profileState.Settings.Paths.Sum(x =>
        {
            if (_fileService.IsDirectory(x))
                return _fileService.GetDirectorySize(x, profileState.Settings.LogsLookupPattern, recursive: true);
            return _fileService.GetFileDetails(x).Length;
        });

        lock (profileState)
        {
            profileState.LastReadSize = lastReadSize;
        }
    }
}
