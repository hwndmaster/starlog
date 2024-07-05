using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Commands;

internal sealed class SettingsUpdateCommand : ICommandMessage
{
    public SettingsUpdateCommand(Settings settings)
    {
        Settings = settings.NotNull();
    }

    public Settings Settings { get; }
}
