using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.CommandHandlers;

internal sealed class SettingsUpdateCommandHandler : ICommandHandler<SettingsUpdateCommand>
{
    private readonly ISettingsRepository _repo;
    private readonly IEventBus _eventBus;

    public SettingsUpdateCommandHandler(ISettingsRepository repo, IEventBus eventBus)
    {
        _repo = repo.NotNull();
        _eventBus = eventBus.NotNull();
    }

    public Task ProcessAsync(SettingsUpdateCommand command)
    {
        _repo.Store(command.Settings);

        _eventBus.Publish(new SettingsUpdatedEvent(command.Settings));

        return Task.CompletedTask;
    }
}
