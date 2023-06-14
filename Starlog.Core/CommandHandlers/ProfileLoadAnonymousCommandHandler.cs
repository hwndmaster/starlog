using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.CommandHandlers;

internal sealed class ProfileLoadAnonymousCommandHandler : ICommandHandler<ProfileLoadAnonymousCommand, Profile>
{
    private readonly IProfileRepository _profileRepo;

    public ProfileLoadAnonymousCommandHandler(IProfileRepository profileRepo)
    {
        _profileRepo = profileRepo.NotNull();
    }

    public Task<Profile> ProcessAsync(ProfileLoadAnonymousCommand command)
    {
        var profile = new Profile
        {
            Id = Profile.AnonymousProfileId,
            Name = "Unnamed",
            Path = command.Path,
            Settings = command.Settings
        };

        _profileRepo.SetAnonymous(profile);

        return Task.FromResult(profile);
    }
}
