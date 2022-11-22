using Genius.Atom.Infrastructure.Commands;

namespace Genius.Starlog.Core.Commands;

public sealed class ProfileCreateCommand : ProfileUpdatableData, ICommandMessageExchange<Guid>
{
}
