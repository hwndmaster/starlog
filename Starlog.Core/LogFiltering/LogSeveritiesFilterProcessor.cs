using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFiltering;

public sealed class LogSeveritiesFilterProcessor : IFilterProcessor
{
    public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log)
    {
        var filter = (LogSeveritiesProfileFilter)profileFilter;

        return filter.LogSeverities.Contains(log.Level.Severity);
    }
}
