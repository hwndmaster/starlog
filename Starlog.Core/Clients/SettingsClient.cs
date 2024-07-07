using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.Clients;

/// <summary>
///   A client for managing application settings.
/// </summary>
public interface ISettingsClient
{
    ProfilesViewSettings GetProfilesViewSettings();
    Task UpdateAsync(Settings settings);
    Task UpdateProfilesViewSettingsAsync(ProfilesViewSettings profilesViewSettings);
}

internal sealed class SettingsClient(
    ICommandBus _commandBus, ISettingsQueryService _query)
    : ISettingsClient
{
    public ProfilesViewSettings GetProfilesViewSettings()
    {
        return _query.Get().ProfilesView;
    }

    public async Task UpdateAsync(Settings settings)
    {
        await _commandBus.SendAsync(new SettingsUpdateCommand(settings));
    }

    public async Task UpdateProfilesViewSettingsAsync(ProfilesViewSettings profilesViewSettings)
    {
        var settings = _query.Get();
        settings.ProfilesView = profilesViewSettings;
        await UpdateAsync(settings);
    }
}
