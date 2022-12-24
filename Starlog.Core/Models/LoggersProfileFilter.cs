namespace Genius.Starlog.Core.Models;

public sealed class LoggersProfileFilter : ProfileFilterBase
{
    public LoggersProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    public bool Exclude { get; set; }

    public string[] LoggerNames { get; set; } = Array.Empty<string>();
}
