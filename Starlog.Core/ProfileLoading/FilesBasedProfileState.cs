using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.ProfileLoading;

internal sealed class FilesBasedProfileState : ProfileStateBase
{
    public required bool IsFileBasedProfile { get; init; }
    public required IFileBasedProfileSettings Settings { get; init; }
    public long LastReadSize { get; set; } = 0;
}
