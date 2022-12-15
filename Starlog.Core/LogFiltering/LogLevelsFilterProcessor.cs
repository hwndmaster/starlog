using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFiltering;

public sealed class LogLevelsFilterProcessor : IFilterProcessor
{
    public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log)
    {
        var filter = (LogLevelsProfileFilter)profileFilter;

        return filter.LogLevels.Contains(log.Level.Name, StringComparer.OrdinalIgnoreCase);
    }
}
