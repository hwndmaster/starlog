namespace Genius.Starlog.Core.Models;

public sealed class ThreadProfileFilter : ProfileFilterBase
{
    public ThreadProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    public string Thread { get; set; } = string.Empty;
}
