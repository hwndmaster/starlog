using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

internal abstract class ProfileStateBase
{
    public required Profile Profile { get; init; }
}
