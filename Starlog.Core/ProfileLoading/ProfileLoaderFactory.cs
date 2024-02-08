using Genius.Atom.Infrastructure.Events;
using Genius.Atom.Infrastructure.Io;
using Genius.Atom.Infrastructure.Tasks;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core.ProfileLoading;

internal interface IProfileLoaderFactory
{
    IProfileLoader? Create(Profile profile);
}

internal sealed class ProfileLoaderFactory : IProfileLoaderFactory
{
    private readonly IDirectoryMonitor _directoryMonitor;
    private readonly IEventBus _eventBus;
    private readonly IFileService _fileService;
    private readonly IFileSystemWatcherFactory _fileSystemWatcherFactory;
    private readonly ILogCodecContainerInternal _logCodecContainer;
    private readonly ILogger<FileBasedProfileLoader> _fileBasedLogger;
    private readonly ISynchronousScheduler _scheduler;

    public ProfileLoaderFactory(
        IDirectoryMonitor directoryMonitor,
        IEventBus eventBus,
        IFileService fileService,
        IFileSystemWatcherFactory fileSystemWatcherFactory,
        ILogCodecContainerInternal logCodecContainer,
        ILogger<FileBasedProfileLoader> fileBasedLogger,
        ISynchronousScheduler scheduler)
    {
        _directoryMonitor = directoryMonitor.NotNull();
        _eventBus = eventBus.NotNull();
        _fileService = fileService.NotNull();
        _fileSystemWatcherFactory = fileSystemWatcherFactory.NotNull();
        _logCodecContainer = logCodecContainer.NotNull();
        _fileBasedLogger = fileBasedLogger.NotNull();
        _scheduler = scheduler.NotNull();
    }

    public IProfileLoader? Create(Profile profile)
    {
        if (profile.Settings is PlainTextProfileSettings)
        {
            return new FileBasedProfileLoader(_directoryMonitor, _eventBus, _fileService, _fileSystemWatcherFactory, _logCodecContainer, _fileBasedLogger, _scheduler);
        }

        return null;
    }
}
