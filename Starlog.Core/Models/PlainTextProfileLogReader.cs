namespace Genius.Starlog.Core.Models;

public sealed class PlainTextProfileLogReader : ProfileLogReaderBase
{
    public PlainTextProfileLogReader(LogReader logReader)
        : base(logReader)
    {
    }

    public string LineRegex { get; set; } = string.Empty;
}
