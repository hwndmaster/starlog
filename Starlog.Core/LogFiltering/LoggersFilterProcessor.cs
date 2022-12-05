using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

public sealed class LoggersFilterProcessor : IFilterProcessor
{
    public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log)
    {
        var filter = (LoggersProfileFilter)profileFilter;

        return filter.LoggerNames.Contains(log.Logger.Name, StringComparer.OrdinalIgnoreCase);
    }
}
