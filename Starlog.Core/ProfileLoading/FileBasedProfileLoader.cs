using System.Reactive;
using System.Reactive.Disposables;
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

// TODO: Cover with unit tests
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

        _logger.LogDebug("Loading profile: {profileId}", profile.Id);

        if (profile.Settings is not IFileBasedProfileSettings fileBasedProfileSettings)
            throw new InvalidOperationException("Profile is not file based as expected in this routine");

        if (!_fileService.PathExists(fileBasedProfileSettings.Path))
            throw new InvalidOperationException($"The path '{fileBasedProfileSettings.Path}' couldn't be found.");

        var isFileBasedProfile = !_fileService.IsDirectory(fileBasedProfileSettings.Path);

        IEnumerable<string> files;
        if (isFileBasedProfile)
        {
            files = fileBasedProfileSettings.Path.Split('|').ToArray();
        }
        else
        {
            files = _fileService.EnumerateFiles(fileBasedProfileSettings.Path, fileBasedProfileSettings.LogsLookupPattern, new EnumerationOptions());
        }
        var tasks = files.Select(async file => await LoadFileAsync(profile, file, logContainer));
        await Task.WhenAll(tasks);

        return new FilesBasedProfileState
        {
            Profile = profile,
            IsFileBasedProfile = isFileBasedProfile,
            Settings = fileBasedProfileSettings
        };
    }

    public IDisposable StartProfileMonitoring(ProfileStateBase profileState, ILogContainerWriter logContainer, Subject<Unit> unknownChangesDetectedSubject)
    {
        if (profileState is not FilesBasedProfileState filesBasedProfileState)
            throw new InvalidOperationException("Cannot start profile monitoring, since it is not file based.");

        var isFileBasedProfile = !_fileService.IsDirectory(filesBasedProfileState.Settings.Path);

        var disposer = new Disposer();
        var watchPath = isFileBasedProfile ? Path.GetDirectoryName(filesBasedProfileState.Settings.Path).NotNull() : filesBasedProfileState.Settings.Path;
        var watchFilter = isFileBasedProfile ? Path.GetFileName(filesBasedProfileState.Settings.Path) : filesBasedProfileState.Settings.LogsLookupPattern;
        var fileWatcher = _fileSystemWatcherFactory.Create(watchPath, watchFilter, increaseBuffer: true)
            ?? throw new InvalidOperationException("Couldn't run file watcher for the path: " + watchPath);

        disposer.Add(fileWatcher);

        fileWatcher.Created.Subscribe(args => FileWatcher_CreatedOrChangedOrDeleted(filesBasedProfileState, logContainer, args)).DisposeWith(disposer);
        fileWatcher.Changed.Subscribe(args => FileWatcher_CreatedOrChangedOrDeleted(filesBasedProfileState, logContainer, args)).DisposeWith(disposer);
        fileWatcher.Renamed.Subscribe(args => FileWatcher_Renamed(logContainer, args)).DisposeWith(disposer);
        fileWatcher.Deleted.Subscribe(args => FileWatcher_CreatedOrChangedOrDeleted(filesBasedProfileState, logContainer, args)).DisposeWith(disposer);
        fileWatcher.Error.Subscribe(FileWatcher_Error).DisposeWith(disposer);

        if (!isFileBasedProfile)
        {
            _directoryMonitor.StartMonitoring(filesBasedProfileState.Settings.Path, filesBasedProfileState.Settings.LogsLookupPattern)
                .DisposeWith(disposer);
            _directoryMonitor.Pulse.Subscribe(async size => await DirectoryMonitor_Pulse(filesBasedProfileState, size, unknownChangesDetectedSubject).ConfigureAwait(false))
                .DisposeWith(disposer);
        }

        if (!filesBasedProfileState.IsFileBasedProfile)
        {
            UpdateLastReadSize(filesBasedProfileState);
        }

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

        if (profile.Settings is not PlainTextProfileSettings plainTextProfileSettings)
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

    private async Task DirectoryMonitor_Pulse(FilesBasedProfileState profileState, long profileDirectorySize, Subject<Unit> unknownChangesDetectedSubject)
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
        _logger.LogDebug("File {fullPath} was {changeType}", e.FullPath, e.ChangeType);

        UpdateLastReadSize(profileState);

        if (profileState.IsFileBasedProfile && !e.FullPath.Equals(profileState.Settings.Path))
        {
            // TODO: Cover with unit tests
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
                    // TODO: Cover with unit tests
                    _scheduler.Schedule(async () => await LoadFileAsync(profileState.Profile, e.FullPath, logContainer));
                }
            });
        }
        else if (e.ChangeType == WatcherChangeTypes.Deleted)
        {
            // TODO: Cover with unit tests
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
        if (profileState.IsFileBasedProfile)
        {
            // Not relevant for file-based profiles
            return;
        }

        var lastReadSize = _fileService.GetDirectorySize(profileState.Settings.Path, profileState.Settings.LogsLookupPattern, recursive: true);
        lock (profileState)
        {
            profileState.LastReadSize = lastReadSize;
        }
    }
}
