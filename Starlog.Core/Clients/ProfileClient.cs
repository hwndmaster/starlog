using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core.Commands;

namespace Genius.Starlog.Core.Clients;

/// <summary>
///   A client for managing profiles.
/// </summary>
public interface IProfileClient
{
    Task DeleteAsync(Guid profileId);
}

internal sealed class ProfileClient(
    ICommandBus _commandBus)
    : IProfileClient
{
    public async Task DeleteAsync(Guid profileId)
    {
        await _commandBus.SendAsync(new ProfileDeleteCommand(profileId));
    }
}
