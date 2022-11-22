using Genius.Atom.Data.Persistence;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core.Repositories;

public interface ISettingsRepository
{
    Settings Get();
    void Store(Settings settings);
}

internal sealed class SettingsRepository : ISettingsRepository
{
    private readonly IEventBus _eventBus;
    private readonly IJsonPersister _persister;
    private readonly ILogger<SettingsRepository> _logger;

    private const string FILENAME = @".\settings.json";
    private Settings _settings;

    public SettingsRepository(IEventBus eventBus, IJsonPersister persister, ILogger<SettingsRepository> logger)
    {
        _persister = persister;
        _logger = logger;
        _eventBus = eventBus;

        _settings = _persister.Load<Settings>(FILENAME) ?? CreateDefaultSettings();
    }

    public Settings Get() => _settings;

    public void Store(Settings settings)
    {
        _settings = settings.NotNull(nameof(settings));

        _persister.Store(FILENAME, settings);

        _eventBus.Publish(new SettingsUpdatedEvent(settings));

        _logger.LogInformation("Settings updated.");
    }

    private static Settings CreateDefaultSettings()
        => new() {
            AutoLoadPreviouslyOpenedProfile = false
        };
}
