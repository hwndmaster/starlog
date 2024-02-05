using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.CommandHandlers;

internal sealed class ProfileCreateOrUpdateCommandHandler :
    ICommandHandler<ProfileCreateCommand, Guid>,
    ICommandHandler<ProfileUpdateCommand>
{
    private readonly IProfileRepository _profileRepo;
    private readonly IProfileQueryService _profileQuery;
    private readonly IEventBus _eventBus;

    public ProfileCreateOrUpdateCommandHandler(IProfileRepository profileRepo, IProfileQueryService profileQuery, IEventBus eventBus)
    {
        _profileRepo = profileRepo.NotNull();
        _profileQuery = profileQuery.NotNull();
        _eventBus = eventBus.NotNull();
    }

    public async Task<Guid> ProcessAsync(ProfileCreateCommand command)
    {
        var profile = new Profile
        {
            Name = command.Name,
            Settings = command.Settings
        };
        await _profileRepo.StoreAsync(profile);

        _eventBus.Publish(new ProfilesAffectedEvent());

        return profile.Id;
    }

    public async Task ProcessAsync(ProfileUpdateCommand command)
    {
        var profile = await _profileQuery.FindByIdAsync(command.ProfileId);
        Guard.NotNull(profile);

        profile.Name = command.Name;
        profile.Settings = command.Settings;

        await _profileRepo.StoreAsync(profile);

        _eventBus.Publish(new ProfilesAffectedEvent());
    }
}
