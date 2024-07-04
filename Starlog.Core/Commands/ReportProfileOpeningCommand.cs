using Genius.Atom.Infrastructure.Commands;

namespace Genius.Starlog.Core.Commands;

public sealed class ReportProfileOpeningCommand : ICommandMessage
{
    public ReportProfileOpeningCommand(Guid profileId)
    {
        ProfileId = profileId;
    }

    public Guid ProfileId { get; }
}
