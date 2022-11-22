using Genius.Atom.Infrastructure.Commands;

namespace Genius.Starlog.Core.Commands;

public sealed class ProfileUpdateCommand : ProfileUpdatableData, ICommandMessage
{
    public ProfileUpdateCommand(Guid profileId)
    {
        ProfileId = profileId;
    }

    public Guid ProfileId { get; }
}
