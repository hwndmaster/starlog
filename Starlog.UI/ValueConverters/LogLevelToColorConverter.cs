using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Genius.Starlog.UI.ViewModels;

namespace Genius.Starlog.UI.ValueConverters;

public sealed class LogLevelToColorConverter : IValueConverter
{
    private readonly Color _standardColor;

    public LogLevelToColorConverter(FrameworkElement anyElement)
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
            Core.LogFlow.LogSeverity.Minor => Colors.DimGray,
            Core.LogFlow.LogSeverity.Attention => Colors.Yellow,
            Core.LogFlow.LogSeverity.Major => Colors.Red,
            Core.LogFlow.LogSeverity.Critical => Colors.DarkRed,
            _ => _standardColor,
        };

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
