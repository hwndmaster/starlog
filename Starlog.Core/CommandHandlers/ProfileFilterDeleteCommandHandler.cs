using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.CommandHandlers;

internal sealed class ProfileFilterDeleteCommandHandler: ICommandHandler<ProfileFilterDeleteCommand>
{
    private readonly IProfileRepository _profileRepo;
    private readonly IProfileQueryService _profileQuery;
    private readonly IEventBus _eventBus;

    public ProfileFilterDeleteCommandHandler(IProfileRepository profileRepo, IProfileQueryService profileQuery, IEventBus eventBus)
    {
        _profileRepo = profileRepo.NotNull();
        _profileQuery = profileQuery.NotNull();
        _eventBus = eventBus.NotNull();
    }

    public async Task ProcessAsync(ProfileFilterDeleteCommand command)
    {
        var profile = await _profileQuery.FindByIdAsync(command.ProfileId);
        Guard.NotNull(profile);

        var filterIndex = profile.Filters.ToList().FindIndex(x => x.Id == command.ProfileFilterId);
        if (filterIndex == -1)
        {
            return;
        }

        profile.Filters.RemoveAt(filterIndex);
        await _profileRepo.StoreAsync(profile);

        _eventBus.Publish(new ProfilesAffectedEvent());
    }
}
