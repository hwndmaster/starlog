using System.Collections.Immutable;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.ProfileLoading;

internal sealed class FilesBasedProfileState : ProfileStateBase
{
    public required IFileBasedProfileSettings Settings { get; init; }
    public long LastReadSize { get; set; }
    public ImmutableArray<WatchingDirectory> WatchingDirectories { get; init; }

    /// <summary>
    ///   A record for holding watching directory information.
    /// </summary>
    /// <param name="Path">A path to the directory, trimmed at the end from '/' and '\' symbols.</param>
    /// <param name="IsFileBased">Indicates whether the path is for file or not.</param>
    /// <param name="ForFiles">
    ///   Contains an array of full file names (including path).
    ///   NOTE: If array is not empty, the <paramref name="Path"/> is file-based, otherwise it is directory-based.
    /// </param>
    internal record WatchingDirectory(string Path, bool IsFileBased, ImmutableArray<string> ForFiles);
}
