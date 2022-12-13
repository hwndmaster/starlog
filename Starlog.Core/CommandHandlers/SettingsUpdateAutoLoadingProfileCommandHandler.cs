using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.CommandHandlers;

internal sealed class SettingsUpdateAutoLoadingProfileCommandHandler : ICommandHandler<SettingsUpdateAutoLoadingProfileCommand>
{
    private readonly ISettingsRepository _repo;

    public SettingsUpdateAutoLoadingProfileCommandHandler(ISettingsRepository repo)
    {
        _repo = repo.NotNull();
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

        return Task.CompletedTask;
    }
}
