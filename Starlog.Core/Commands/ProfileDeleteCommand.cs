using Genius.Atom.Infrastructure.Commands;

namespace Genius.Starlog.Core.Commands;

public sealed class ProfileDeleteCommand : ICommandMessage
{
    public ProfileDeleteCommand(Guid profileId)
    {
        ProfileId = profileId;
    }

    public Guid ProfileId { get; }
}
