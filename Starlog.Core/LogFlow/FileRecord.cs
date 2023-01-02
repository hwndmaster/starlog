using System.Diagnostics.Contracts;

namespace Genius.Starlog.Core.LogFlow;

/// <summary>
///   A class contains information about a reading log file.
/// </summary>
public sealed class FileRecord
{
    public FileRecord(string fullPath, long lastReadOffset)
    {
        FullPath = fullPath.NotNull();

        var fileName = Path.GetFileName(fullPath);
        FileName = fileName.NotNull();

        LastReadOffset = lastReadOffset;
    }

    [Pure]
    public FileRecord WithNewName(string newFullPath)
    {
        return new FileRecord(newFullPath, LastReadOffset);
    }

    /// <summary>
    ///   The full path to the log file.
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    ///   The name of the log file.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    ///   The offset when the last reading was finished.
    /// </summary>
    public long LastReadOffset { get; set; }

    /// <summary>
    ///   The file artifacts.
    /// </summary>
    public FileArtifacts? Artifacts { get; set; }
}
