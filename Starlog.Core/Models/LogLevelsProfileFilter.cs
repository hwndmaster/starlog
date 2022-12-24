namespace Genius.Starlog.Core.Models;

public sealed class LogLevelsProfileFilter : ProfileFilterBase
{
    public LogLevelsProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    public bool Exclude { get; set; }

    public string[] LogLevels { get; set; } = Array.Empty<string>();
}
