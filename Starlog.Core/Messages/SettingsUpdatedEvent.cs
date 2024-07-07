using Genius.Starlog.Core.Models;
using Genius.Atom.Infrastructure.Events;

namespace Genius.Starlog.Core.Messages;

public sealed class SettingsUpdatedEvent : IEventMessage
{
    internal SettingsUpdatedEvent(Settings settings)
    {
        Settings = settings;
    }

    public Settings Settings { get; }
}
