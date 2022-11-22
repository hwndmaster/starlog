using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.CommandHandlers;

internal sealed class ProfileDeleteCommandHandler : ICommandHandler<ProfileDeleteCommand>
{
    private readonly IProfileRepository _profileRepo;
    private readonly IEventBus _eventBus;

    public ProfileDeleteCommandHandler(IProfileRepository profileRepo, IEventBus eventBus)
    {
        _profileRepo = profileRepo.NotNull();
        _eventBus = eventBus.NotNull();
    }

    public async Task ProcessAsync(ProfileDeleteCommand command)
    {
        await _profileRepo.DeleteAsync(command.ProfileId);

        _eventBus.Publish(new ProfilesAffectedEvent());
    }
}
