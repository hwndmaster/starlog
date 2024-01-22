using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Commands;

public readonly record struct MessageParsingCreateOrUpdateCommandResult(Guid? MessageParsingAdded, Guid? MessageParsingUpdated);

public sealed class MessageParsingCreateOrUpdateCommand : ICommandMessageExchange<MessageParsingCreateOrUpdateCommandResult>
{
    public required Guid ProfileId { get; init; }
    public required MessageParsing MessageParsing { get; init; }
}
