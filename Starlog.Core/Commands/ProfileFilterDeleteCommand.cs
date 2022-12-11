using Genius.Atom.Infrastructure.Commands;

namespace Genius.Starlog.Core.Commands;

public sealed class ProfileFilterDeleteCommand : ICommandMessage
{
    public ProfileFilterDeleteCommand(Guid profileId, Guid profileFilterId)
    {
        ProfileId = profileId;
        ProfileFilterId = profileFilterId;
    }

    public Guid ProfileId { get; }
    public Guid ProfileFilterId { get; }
}
