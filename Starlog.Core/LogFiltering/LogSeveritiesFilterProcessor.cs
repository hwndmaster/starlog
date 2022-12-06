using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

public sealed class LogSeveritiesFilterProcessor : IFilterProcessor
{
    public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log)
    {
        var filter = (LogSeveritiesProfileFilter)profileFilter;

        return filter.LogSeverities.Contains(log.Level.Severity);
    }
}
