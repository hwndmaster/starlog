using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.CommandHandlers;

internal sealed class SettingsUpdateAutoLoadingProfileCommandHandler : ICommandHandler<SettingsUpdateAutoLoadingProfileCommand>
{
    private readonly ISettingsRepository _repo;
    private readonly IEventBus _eventBus;

    public SettingsUpdateAutoLoadingProfileCommandHandler(ISettingsRepository repo, IEventBus eventBus)
    {
        _repo = repo.NotNull();
        _eventBus = eventBus.NotNull();
    }

    public Task ProcessAsync(SettingsUpdateAutoLoadingProfileCommand command)
    {
        var settings = _repo.Get();
        if (!settings.AutoLoadPreviouslyOpenedProfile)
        {
            return Task.CompletedTask;
        }

        settings.AutoLoadProfile = command.ProfileId;
        _repo.Store(settings);

        _eventBus.Publish(new SettingsUpdatedEvent(settings));

        return Task.CompletedTask;
    }
}
