using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFiltering;

public sealed class TimeAgoFilterProcessor : IFilterProcessor
{
    private readonly IDateTime _dateTime;

    public TimeAgoFilterProcessor(IDateTime dateTime)
    {
        _dateTime = dateTime;
    }

    public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log)
    {
        var filter = (TimeAgoProfileFilter)profileFilter;

        return (_dateTime.NowOffset.UtcDateTime + _dateTime.NowOffset.Offset) - log.DateTime < filter.TimeAgo;
    }
}
