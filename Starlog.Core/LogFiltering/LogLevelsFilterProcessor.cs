using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

public sealed class LogLevelsFilterProcessor : IFilterProcessor
{
    public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log)
    {
        var filter = (LogLevelsProfileFilter)profileFilter;

        return filter.LogLevels.Contains(log.Level.Name, StringComparer.OrdinalIgnoreCase);
    }
}
