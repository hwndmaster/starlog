using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.ProfileLoading;

internal abstract class ProfileStateBase
{
    public required Profile Profile { get; init; }
}
