namespace Genius.Starlog.Core.Models;

public sealed class ThreadsProfileFilter : ProfileFilterBase
{
    public ThreadsProfileFilter(LogFilter logFilter)
        : base(logFilter)
    {
    }

    public bool Exclude { get; set; }

    public string[] Threads { get; set; } = Array.Empty<string>();
}
