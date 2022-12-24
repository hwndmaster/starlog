using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFiltering;

public sealed class LoggersFilterProcessor : IFilterProcessor
{
    public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log)
    {
        var filter = (LoggersProfileFilter)profileFilter;

        var result = filter.LoggerNames.Contains(log.Logger.Name, StringComparer.OrdinalIgnoreCase);

        return filter.Exclude ? !result : result;
    }
}
