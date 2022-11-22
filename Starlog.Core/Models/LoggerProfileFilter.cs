namespace Genius.Starlog.Core.Models;

public sealed class LoggerProfileFilter : ProfileFilterBase
{
    public LoggerProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    public string LoggerName { get; set; } = string.Empty;
}
