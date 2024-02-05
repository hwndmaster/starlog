using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Commands;

public sealed class ProfileLoadAnonymousCommand : ICommandMessageExchange<Profile>
{
    public ProfileLoadAnonymousCommand(ProfileSettingsBase settings)
    {
        Settings = settings.NotNull();
    }

    public ProfileSettingsBase Settings { get; }
}
