using Genius.Atom.Infrastructure.Commands;

namespace Genius.Starlog.Core.Commands;

public sealed class SettingsUpdateAutoLoadingProfileCommand : ICommandMessage
{
    public SettingsUpdateAutoLoadingProfileCommand(Guid profileId)
    {
        ProfileId = profileId;
    }

    public Guid ProfileId { get; }
}
