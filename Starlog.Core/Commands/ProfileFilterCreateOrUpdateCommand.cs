using System.Collections.Immutable;
using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Commands;

public readonly record struct ProfileFilterCreateOrUpdateCommandResult(ImmutableArray<Guid> ProfileFiltersAdded, ImmutableArray<Guid> ProfileFiltersUpdated);

public sealed class ProfileFilterCreateOrUpdateCommand : ICommandMessageExchange<ProfileFilterCreateOrUpdateCommandResult>
{
    public required Guid ProfileId { get; init; }
    public required ProfileFilterBase ProfileFilter { get; init; }
}
