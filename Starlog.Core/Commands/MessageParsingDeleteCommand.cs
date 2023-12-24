using Genius.Atom.Infrastructure.Commands;

namespace Genius.Starlog.Core.Commands;

public sealed class MessageParsingDeleteCommand : ICommandMessage
{
    public MessageParsingDeleteCommand(Guid profileId, Guid messageParsingId)
    {
        ProfileId = profileId;
        MessageParsingId = messageParsingId;
    }

    public Guid ProfileId { get; }
    public Guid MessageParsingId { get; }
}
