using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.CommandHandlers;

internal sealed class ReportProfileOpeningCommandHandler : ICommandHandler<ReportProfileOpeningCommand>
{
    private readonly IDateTime _dateTime;
    private readonly IEventBus _eventBus;
    private readonly IProfileQueryService _profileQuery;
    private readonly IProfileRepository _profileRepo;
    private readonly ISettingsRepository _settingsRepo;

    public ReportProfileOpeningCommandHandler(IDateTime dateTime, IEventBus eventBus, IProfileQueryService profileQuery, IProfileRepository profileRepo, ISettingsRepository settingsRepo)
    {
        _dateTime = dateTime.NotNull();
        _eventBus = eventBus.NotNull();
        _profileQuery = profileQuery.NotNull();
        _profileRepo = profileRepo.NotNull();
        _settingsRepo = settingsRepo.NotNull();
    }

    public async Task ProcessAsync(ReportProfileOpeningCommand command)
    {
        var profile = await _profileQuery.FindByIdAsync(command.ProfileId);
        if (profile is null)
        {
            return;
        }

        var lastOpened = _dateTime.Now;
        profile.LastOpened = lastOpened;

        await _profileRepo.StoreAsync(profile);
        _eventBus.Publish(new ProfileLastOpenedUpdatedEvent(command.ProfileId, lastOpened));
        _eventBus.Publish(new ProfilesAffectedEvent());

        UpdateAutoLoadPreviouslyOpenedProfile(command.ProfileId);
    }

    private void UpdateAutoLoadPreviouslyOpenedProfile(Guid profileId)
    {
        var settings = _settingsRepo.Get();
        if (settings.AutoLoadPreviouslyOpenedProfile)
        {
            settings.AutoLoadProfile = profileId;
            _settingsRepo.Store(settings);
        }
    }
}
