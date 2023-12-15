using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.ValueConverters;

public sealed class LogLevelToColorConverter : IValueConverter
{
    internal static readonly Color ColorForMinor = Colors.SlateGray;
    internal static readonly Color ColorForWarning = Colors.Gold;
    internal static readonly Color ColorForMajor = Colors.IndianRed;
    internal static readonly Color ColorForCritical = Colors.Red;

    private readonly Color _standardColor;

    public LogLevelToColorConverter(FrameworkElement anyElement)
    {
        _standardColor = (Color)anyElement.FindResource("MahApps.Colors.ThemeForeground");
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ILogItemViewModel vm)
        {
            throw new InvalidOperationException("Expected a view model of ILogItemViewModel type.");
        }

        var color = vm.Record.Level.Name.ToLowerInvariant() switch
        {
            "verbose" or "debug" or "trace" or "statistics" => ColorForMinor,
            "warn" or "warning" => ColorForWarning,
            "err" or "error" or "exception" => ColorForMajor,
            "fatal" => ColorForCritical,
            _ => _standardColor
        };

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
