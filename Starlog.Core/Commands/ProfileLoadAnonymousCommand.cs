using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Commands;

public sealed class ProfileLoadAnonymousCommand : ICommandMessageExchange<Profile>
{
    public ProfileLoadAnonymousCommand(string path, ProfileSettings settings)
    {
        Path = path.NotNull();
        Settings = settings.NotNull();
    }

    public string Path { get; }
    public ProfileSettings Settings { get; }
}
