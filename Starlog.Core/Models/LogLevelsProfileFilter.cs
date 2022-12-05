namespace Genius.Starlog.Core.Models;

public sealed class LogLevelsProfileFilter : ProfileFilterBase
{
    public LogLevelsProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    public LogSeverity[] LogLevels { get; set; } = Array.Empty<LogSeverity>();
}
