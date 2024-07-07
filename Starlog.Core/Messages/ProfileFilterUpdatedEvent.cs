using Genius.Atom.Infrastructure.Events;

namespace Genius.Starlog.Core.Messages;

public sealed class ProfileFilterUpdatedEvent : IEventMessage
{
    internal ProfileFilterUpdatedEvent(Guid profileFilterId)
    {
        ProfileFilterId = profileFilterId;
    }

    public Guid ProfileFilterId { get; }
}
