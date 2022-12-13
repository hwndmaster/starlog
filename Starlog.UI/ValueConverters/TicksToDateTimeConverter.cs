using System.Globalization;
using System.Windows.Data;

namespace Genius.Starlog.UI.ValueConverters;

public sealed class TicksToDateTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        long ticks = 0;
        if (value is string stringValue)
        {
            long.TryParse(stringValue, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out ticks);
        }
        else if (value is double doubleValue)
        {
            ticks = (long)doubleValue;
        }

        var dt = new DateTimeOffset(ticks, TimeSpan.Zero);
        return dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
