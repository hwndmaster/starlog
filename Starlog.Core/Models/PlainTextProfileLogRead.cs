namespace Genius.Starlog.Core.Models;

public sealed class PlainTextProfileLogRead : ProfileLogReadBase
{
    public PlainTextProfileLogRead(LogReader logReader)
        : base(logReader)
    {
    }

    public string LineRegex { get; set; } = string.Empty;
}
