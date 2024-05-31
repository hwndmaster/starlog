using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFiltering;

public sealed class LogLevelsFilterProcessor : IFilterProcessor
{
    public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log)
    {
        Guard.NotNull(profileFilter);

        var filter = (LogLevelsProfileFilter)profileFilter;
        var result = filter.LogLevels.Contains(log.Level.Name, StringComparer.OrdinalIgnoreCase);

        return filter.Exclude ? !result : result;
    }
}
