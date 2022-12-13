using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Genius.Starlog.UI.ValueConverters;

public sealed class TickRangeToTimeConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2)
        {
            return DependencyProperty.UnsetValue;
        }

        if (values[0] is string sv1 && values[1] is string sv2)
        {
            if (!long.TryParse(sv1, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var ticksFrom))
            {
                return DependencyProperty.UnsetValue;
            }
            if (!long.TryParse(sv2, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var ticksTo))
            {
                return DependencyProperty.UnsetValue;
            }
            var span = new TimeSpan(ticksTo - ticksFrom);

            StringBuilder sb = new();
            if (span.TotalMinutes >= 1)
            {
                sb.Append(Math.Floor(span.TotalMinutes));
                sb.Append(" min ");
            }
            sb.Append(span.Seconds);
            sb.Append(" sec ");
            sb.Append(span.Milliseconds);
            sb.Append(" ms");

            return sb.ToString();
        }

        return DependencyProperty.UnsetValue;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
