namespace Genius.Starlog.Core.LogFlow;

public sealed class FileRecord
{
    public FileRecord(string fullPath, string fileName, long lastReadOffset)
    {
        FullPath = fullPath.NotNull();
        FileName = fileName.NotNull();
        LastReadOffset = lastReadOffset;
    }

    public string FullPath { get; }
    public string FileName { get; }
    public long LastReadOffset { get; set; }
}
