using Genius.Atom.Infrastructure.Events;

namespace Genius.Starlog.Core.Messages;

public sealed class ProfileLastOpenedUpdatedEvent : IEventMessage
{
    internal ProfileLastOpenedUpdatedEvent(Guid profileId, DateTime lastOpened)
    {
        ProfileId = profileId;
        LastOpened = lastOpened;
    }

    public Guid ProfileId { get; }
    public DateTime LastOpened { get; }
}
