namespace Genius.Starlog.Core.LogFlow;

/// <summary>
///   A class contains information about a reading log file.
/// </summary>
public sealed class FileRecord
{
    public FileRecord(string fullPath, string fileName, long lastReadOffset)
    {
        FullPath = fullPath.NotNull();
        FileName = fileName.NotNull();
        LastReadOffset = lastReadOffset;
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
