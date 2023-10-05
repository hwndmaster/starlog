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
    private readonly IEventBus _eventBus;
    private readonly IFileService _fileService;
    private readonly IProfileLoader _profileLoader;
    private readonly ISynchronousScheduler _scheduler;
    private readonly ILogger<LogContainer> _logger;
    private readonly IFileSystemWatcher _fileWatcher;
    private readonly Subject<Unit> _profileClosed = new();
    private readonly Subject<Profile> _profileChanged = new();

    public CurrentProfileLogContainer(
        IEventBus eventBus,
        IFileService fileService,
        IFileSystemWatcher fileWatcher,
        IProfileLoader profileLoader,
        ISynchronousScheduler scheduler,
        ILogger<LogContainer> logger)
    {
        _eventBus = eventBus.NotNull();
        _fileService = fileService.NotNull();
        _fileWatcher = fileWatcher.NotNull();
        _profileLoader = profileLoader.NotNull();
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

        var profileLoaded = await _profileLoader.LoadProfileAsync(profile, this).ConfigureAwait(false);

        if (profileLoaded)
        {
            Profile = profile.NotNull();

            var isFile = !_fileService.IsDirectory(profile.Path);

            if (!_fileWatcher.StartListening(
                path: isFile ? Path.GetDirectoryName(profile.Path).NotNull() : profile.Path,
                filter: isFile ? Path.GetFileName(profile.Path) : "*.*"))
            {
                // TODO: Cover with unit tests
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
        Clear();
        _profileClosed.OnNext(Unit.Default);
    }

    public void Dispose()
    {
        _fileWatcher.Dispose();
    }

    private void FileWatcher_CreatedOrChanged(FileSystemEventArgs e)
    {
        if (Profile is null)
        {
            // No active profile
            return;
        }

        _logger.LogDebug("File {fullPath} is {changeType}", e.FullPath, e.ChangeType);

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
        _logger.LogDebug("File {OldFullPath} is renamed to {FullPath}", e.OldFullPath, e.FullPath);

        var previousFileRecord = RemoveFile(e.OldFullPath, triggerEvent: false);

        if (previousFileRecord is null)
        {
            // TODO: Cover with unit tests
            return;
        }

        RenameFile(previousFileRecord, e.FullPath);
    }

    private void FileWatcher_Error(ErrorEventArgs e)
    {
        var ex = e.GetException();
        _logger.LogError(ex, ex.Message);
    }

    public Profile? Profile { get; private set; }
    public IObservable<Unit> ProfileClosed => _profileClosed;
    public IObservable<Profile> ProfileChanged => _profileChanged;
}
