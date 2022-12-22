using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Data;

namespace Genius.Starlog.UI.ValueConverters;

[ExcludeFromCodeCoverage]
public sealed class AppIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return App.Current.FindResource(value.ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
