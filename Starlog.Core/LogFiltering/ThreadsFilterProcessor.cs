using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

public sealed class ThreadsFilterProcessor : IFilterProcessor
{
    public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log)
    {
        var filter = (ThreadsProfileFilter)profileFilter;

        return filter.Threads.Contains(log.Thread, StringComparer.OrdinalIgnoreCase);
    }
}
