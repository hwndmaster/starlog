using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFiltering;

public sealed class TimeRangeFilterProcessor : IFilterProcessor
{
    public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log)
    {
        var filter = (TimeRangeProfileFilter)profileFilter;

        return log.DateTime >= filter.TimeFrom && log.DateTime <= filter.TimeTo;
    }
}
