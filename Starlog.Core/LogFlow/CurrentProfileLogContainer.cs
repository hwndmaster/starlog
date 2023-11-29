using System.Reactive;
using System.Reactive.Subjects;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.Infrastructure.Io;
using Genius.Atom.Infrastructure.Tasks;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core.LogFlow;

internal sealed class CurrentProfileLogContainer : LogContainer, ICurrentProfile, IDisposable
{
    private readonly IDirectoryMonitor _directoryMonitor;
    private readonly IEventBus _eventBus;
    private readonly IFileService _fileService;
    private readonly IFileSystemWatcher _fileWatcher;
    private readonly IProfileLoader _profileLoader;
    private readonly ISynchronousScheduler _scheduler;
    private readonly ILogger<LogContainer> _logger;
    private readonly Subject<Unit> _profileClosed = new();
    private readonly Subject<Profile> _profileChanged = new();
    private readonly Subject<Unit> _unknownChangesDetected = new();
    private long _lastReadSize = 0;

    public CurrentProfileLogContainer(
        IEventBus eventBus,
        IDirectoryMonitor directoryMonitor,
        IFileService fileService,
        IFileSystemWatcher fileWatcher,
        IProfileLoader profileLoader,
        ISynchronousScheduler scheduler,
        ILogger<LogContainer> logger)
    {
        _eventBus = eventBus.NotNull();
        _directoryMonitor = directoryMonitor.NotNull();
        _fileService = fileService.NotNull();
        _fileWatcher = fileWatcher.NotNull();
        _profileLoader = profileLoader.NotNull();
        _scheduler = scheduler.NotNull();
        _logger = logger.NotNull();

        _fileWatcher.IncreaseBuffer();
        _fileWatcher.Created.Subscribe(FileWatcher_CreatedOrChangedOrDeleted);
        _fileWatcher.Changed.Subscribe(FileWatcher_CreatedOrChangedOrDeleted);
        _fileWatcher.Renamed.Subscribe(FileWatcher_Renamed);
        _fileWatcher.Deleted.Subscribe(FileWatcher_CreatedOrChangedOrDeleted);
        _fileWatcher.Error.Subscribe(FileWatcher_Error);

        _directoryMonitor.Pulse.Subscribe(async size => await DirectoryMonitor_Pulse(size).ConfigureAwait(false));
    }

    private async Task DirectoryMonitor_Pulse(long profileDirectorySize)
    {
        if (_lastReadSize != profileDirectorySize)
        {
            // TODO: Something still doesn't work well here
            await Task.Delay(5000).ConfigureAwait(false);
            if (_lastReadSize != profileDirectorySize)
            {
                _lastReadSize = profileDirectorySize;
                _unknownChangesDetected.OnNext(Unit.Default);
            }
        }
    }

    public async Task LoadProfileAsync(Profile profile)
    {
        _logger.LogDebug("Loading profile: {profileId}", profile.Id);

        CloseProfile();

        var profileLoaded = await _profileLoader.LoadProfileAsync(profile, this).ConfigureAwait(false);

        if (profileLoaded)
        {
            Profile = profile.NotNull();

            var isFile = !_fileService.IsDirectory(profile.Path);

            UpdateLastReadSize();

            if (!_fileWatcher.StartListening(
                path: isFile ? Path.GetDirectoryName(profile.Path).NotNull() : profile.Path,
                filter: isFile ? Path.GetFileName(profile.Path) : "*.*"))
            {
                // TODO: Cover with unit tests
                _eventBus.Publish(new ProfileLoadingErrorEvent(profile, $"Couldn't start file monitoring over the profile path: '{profile.Path}'."));
            }

            if (!isFile)
            {
                _directoryMonitor.StartMonitoring(Profile.Path);
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
        _directoryMonitor.StopMonitoring();
        _fileWatcher.StopListening();
        Profile = null;
        Clear();
        _profileClosed.OnNext(Unit.Default);
    }

    public void Dispose()
    {
        _directoryMonitor.Dispose();
        _fileWatcher.Dispose();
    }

    private void FileWatcher_CreatedOrChangedOrDeleted(FileSystemEventArgs e)
    {
        if (Profile is null)
        {
            // No active profile
            return;
        }

        _logger.LogDebug("File {fullPath} was {changeType}", e.FullPath, e.ChangeType);

        UpdateLastReadSize();

        if (e.ChangeType == WatcherChangeTypes.Created)
        {
            _scheduler.ScheduleAsync(async () => await _profileLoader.LoadFileAsync(Profile, e.FullPath, this));
        }
        else if (e.ChangeType == WatcherChangeTypes.Changed)
        {
            _scheduler.ScheduleAsync(async () =>
            {
                var fileRecord = GetFile(e.FullPath);
                if (fileRecord is not null)
                {
                    using var fileStream = _fileService.OpenReadNoLock(e.FullPath);
                    fileStream.Seek(fileRecord.LastReadOffset, SeekOrigin.Begin);
                    _logger.LogDebug("File {FileName} seeking to {LastReadOffset}", fileRecord.FileName, fileRecord.LastReadOffset);
                    await _profileLoader.ReadLogsAsync(Profile, fileStream, fileRecord, this).ConfigureAwait(false);
                }
                else
                {
                    // TODO: Cover with unit tests
                    _scheduler.ScheduleAsync(async () => await _profileLoader.LoadFileAsync(Profile, e.FullPath, this));
                }
            });
        }
        else if (e.ChangeType == WatcherChangeTypes.Deleted)
        {
            // TODO: Cover with unit tests
            RemoveFile(e.FullPath);
        }
    }

    private void FileWatcher_Renamed(RenamedEventArgs e)
    {
        _logger.LogDebug("File {OldFullPath} was renamed to {FullPath}", e.OldFullPath, e.FullPath);

        RenameFile(e.OldFullPath, e.FullPath);
    }

    private void FileWatcher_Error(ErrorEventArgs e)
    {
        var ex = e.GetException();
        _logger.LogError(ex, ex.Message);
    }

    private void UpdateLastReadSize()
    {
        if (Profile is null)
        {
            // No active profile
            return;
        }

        // TODO: Cover with unit tests
        _lastReadSize = _fileService.GetDirectorySize(Profile.Path, recursive: true);
    }

    public Profile? Profile { get; private set; }
    public IObservable<Unit> ProfileClosed => _profileClosed;
    public IObservable<Profile> ProfileChanged => _profileChanged;
    public IObservable<Unit> UnknownChangesDetected => _unknownChangesDetected;
}
