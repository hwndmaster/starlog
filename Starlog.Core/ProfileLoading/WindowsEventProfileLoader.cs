using System.Reactive;
using System.Reactive.Subjects;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core.ProfileLoading;

internal sealed class WindowsEventProfileLoader : IProfileLoader
{
    private readonly IEventBus _eventBus;
    private readonly ILogCodecContainerInternal _logCodecContainer;
    private readonly ILogger<WindowsEventProfileLoader> _logger;

    public WindowsEventProfileLoader(
        IEventBus eventBus,
        ILogCodecContainerInternal logCodecContainer,
        ILogger<WindowsEventProfileLoader> logger)
    {
        _eventBus = eventBus.NotNull();
        _logCodecContainer = logCodecContainer.NotNull();
        _logger = logger.NotNull();
    }

    public async Task<ProfileStateBase> LoadProfileAsync(Profile profile, ILogContainerWriter logContainer)
    {
        Guard.NotNull(profile);
        Guard.NotNull(logContainer);

        if (profile.Settings is not WindowsEventProfileSettings profileSettings)
        {
            throw new InvalidOperationException("Attempted to load Windows Events profile with settings of wrong type: " + profile.Settings.GetType().FullName);
        }

        _logger.LogDebug("Loading profile: {ProfileId}", profile.Id);

        var tp = TracePerf.Start<WindowsEventProfileLoader>(nameof(LoadProfileAsync));

        var logCodecProcessor = _logCodecContainer.FindLogCodecProcessor(profile.Settings);
        var settings = new LogReadingSettings(ReadSourceArtifacts: false);

        foreach (var sourceName in profileSettings.Sources)
        {
            var source = new WindowsEventSource(sourceName);
            using var dummyStream = new MemoryStream();
            var logRecordResult = await logCodecProcessor.ReadAsync(profile, source, dummyStream, settings, logContainer.GetFieldsContainer());
            _logger.LogDebug("WinEvent source {Name} read {RecordsCount} logs", source.Name, logRecordResult.Records.Length);

            if (logRecordResult.Errors.Count > 0)
            {
                var reason = string.Join(Environment.NewLine, logRecordResult.Errors);
                _eventBus.Publish(new ProfileLoadingErrorEvent(profile, reason));
                _logger.LogDebug("WinEvent source {Name} found {RecordsCount} errors\r\n{Reason}", source.Name, logRecordResult.Errors.Count, reason);
            }

            foreach (var logLevel in logRecordResult.LogLevels)
            {
                logContainer.AddLogLevel(logLevel);
            }

            logContainer.AddLogs(logRecordResult.Records);
            logContainer.AddSource(source);
        }

        tp.StopAndReport();

        return new WindowsEventProfileState
        {
            Profile = profile
        };
    }

    public IDisposable StartProfileMonitoring(ProfileStateBase profileState, ILogContainerWriter logContainer, Subject<Unit> unknownChangesDetectedSubject)
    {
        return new Disposer();
    }
}
