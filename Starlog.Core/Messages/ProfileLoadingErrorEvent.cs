using Genius.Starlog.Core.Models;
using Genius.Atom.Infrastructure.Events;

namespace Genius.Starlog.Core.Messages;

public sealed class ProfileLoadingErrorEvent : IEventMessage
{
    public ProfileLoadingErrorEvent(Profile profile, string reason)
    {
        Profile = profile;
        Reason = reason;
    }

    public Profile Profile { get; }
    public string Reason { get; }
}
