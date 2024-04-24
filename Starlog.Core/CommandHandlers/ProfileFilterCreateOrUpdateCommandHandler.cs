using System.Collections.Immutable;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.CommandHandlers;

internal sealed class ProfileFilterCreateOrUpdateCommandHandler: ICommandHandler<ProfileFilterCreateOrUpdateCommand, ProfileFilterCreateOrUpdateCommandResult>
{
    private readonly IProfileRepository _profileRepo;
    private readonly IProfileQueryService _profileQuery;
    private readonly IEventBus _eventBus;

    public ProfileFilterCreateOrUpdateCommandHandler(IProfileRepository profileRepo, IProfileQueryService profileQuery, IEventBus eventBus)
    {
        _profileRepo = profileRepo.NotNull();
        _profileQuery = profileQuery.NotNull();
        _eventBus = eventBus.NotNull();
    }

    public async Task<ProfileFilterCreateOrUpdateCommandResult> ProcessAsync(ProfileFilterCreateOrUpdateCommand command)
    {
        var profile = await _profileQuery.FindByIdAsync(command.ProfileId);
        Guard.NotNull(profile);

        List<Guid> filtersAdded = [];
        List<Guid> filtersUpdated = [];
        var filterIndex = profile.Filters.ToList().FindIndex(x => x.Id == command.ProfileFilter.Id);
        if (filterIndex == -1)
        {
            profile.Filters.Add(command.ProfileFilter);
            filtersAdded.Add(command.ProfileFilter.Id);
        }
        else
        {
            profile.Filters[filterIndex] = command.ProfileFilter;
            filtersUpdated.Add(command.ProfileFilter.Id);
        }
        await _profileRepo.StoreAsync(profile);

        if (filterIndex != -1)
        {
            _eventBus.Publish(new ProfileFilterUpdatedEvent(command.ProfileFilter.Id));
        }
        _eventBus.Publish(new ProfilesAffectedEvent());

        return new ProfileFilterCreateOrUpdateCommandResult(filtersAdded.ToImmutableArray(), filtersUpdated.ToImmutableArray());
    }
}
