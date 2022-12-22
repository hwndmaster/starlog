using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.ValueConverters;

public sealed class LogSeverityToColorConverter : IValueConverter
{
    internal static readonly Color ColorForMinor = Colors.DimGray;
    internal static readonly Color ColorForWarning = Colors.Yellow;
    internal static readonly Color ColorForMajor = Colors.Red;
    internal static readonly Color ColorForCritical = Colors.DarkRed;

    private readonly Color _standardColor;

    public LogSeverityToColorConverter(FrameworkElement anyElement)
    {
        _standardColor = (Color)anyElement.FindResource("MahApps.Colors.ThemeForeground");
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ILogItemViewModel vm)
        {
            throw new InvalidOperationException("Expected a view model of ILogItemViewModel type.");
        }

        var color = vm.Record.Level.Severity switch
        {
            LogSeverity.Minor => ColorForMinor,
            LogSeverity.Warning => ColorForWarning,
            LogSeverity.Major => ColorForMajor,
            LogSeverity.Critical => ColorForCritical,
            _ => _standardColor,
        };

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
