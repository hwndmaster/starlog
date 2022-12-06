using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.ViewModels;

namespace Genius.Starlog.UI.ValueConverters;

public sealed class LogSeverityToColorConverter : IValueConverter
{
    private readonly Color _standardColor;

    public LogSeverityToColorConverter(FrameworkElement anyElement)
    {
        _standardColor = (Color)anyElement.FindResource("MahApps.Colors.ThemeForeground");
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not LogItemViewModel vm)
        {
            throw new InvalidOperationException("Expected a view model of LogItemViewModel type.");
        }

        var color = vm.Record.Level.Severity switch
        {
            LogSeverity.Minor => Colors.DimGray,
            LogSeverity.Attention => Colors.Yellow,
            LogSeverity.Major => Colors.Red,
            LogSeverity.Critical => Colors.DarkRed,
            _ => _standardColor,
        };

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
