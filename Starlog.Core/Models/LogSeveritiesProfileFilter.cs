namespace Genius.Starlog.Core.Models;

public sealed class LogSeveritiesProfileFilter : ProfileFilterBase
{
    public LogSeveritiesProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    public bool Exclude { get; set; }

    public LogSeverity[] LogSeverities { get; set; } = Array.Empty<LogSeverity>();
}
