using System.Globalization;

namespace Genius.Starlog.UI.Helpers;

public static class LogFilterHelpers
{
    private const int MaxNameLength = 50;

    public static string LimitNameLength(string name)
    {
        if (name is not null && name.Length > MaxNameLength)
        {
            return name[..(MaxNameLength - 1)] + "…";
        }

        return name!;
    }

    public static string ProposeNameForStringList(string title, IEnumerable<string> stringList, bool exclude)
    {
        return LimitNameLength((exclude ? "Not " : string.Empty) + title + ": " + string.Join(", ", stringList));
    }

    public static string ProposeNameForTimeRange(DateTimeOffset timeFrom, DateTimeOffset timeTo)
        => ProposeNameForTimeRange(
            new DateTime(timeFrom.UtcTicks, DateTimeKind.Utc),
            new DateTime(timeTo.UtcTicks, DateTimeKind.Utc));

    public static string ProposeNameForTimeRange(DateTime timeFrom, DateTime timeTo)
    {
        return "Time from " + timeFrom.ToString(CultureInfo.CurrentCulture) + " to " + (
            timeTo.Date == timeFrom.Date ? timeTo.ToLongTimeString() : timeTo.ToString(CultureInfo.CurrentCulture)
        );
    }
}
