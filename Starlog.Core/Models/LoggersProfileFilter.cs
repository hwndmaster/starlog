namespace Genius.Starlog.Core.Models;

public sealed class LoggersProfileFilter : ProfileFilterBase
{
    public LoggersProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    public string[] LoggerNames { get; set; } = Array.Empty<string>();
}
