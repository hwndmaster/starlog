using System.Diagnostics.Contracts;

namespace Genius.Starlog.Core.LogFlow;

/// <summary>
///   A class contains information about a reading log file.
/// </summary>
public sealed class FileRecord : LogSourceBase
{
    public FileRecord(string fullPath, long lastReadOffset)
        : this(fullPath.NotNull(), Path.GetFileName(fullPath).NotNull(), lastReadOffset)
    {
    }

    private FileRecord(string fullPath, string fileName, long lastReadOffset)
    {
        FullPath = fullPath;
        FileName = fileName;
        LastReadOffset = lastReadOffset;
    }

    [Pure]
    public override LogSourceBase WithNewName(string newName)
    {
        return new FileRecord(newName, LastReadOffset);
    }

    public override string Name => FullPath;

    public override string DisplayName => FileName;

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
}
