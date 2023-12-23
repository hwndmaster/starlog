using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Media;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.ValueConverters;

public sealed class LogThreadToColorConverter : IValueConverter
{
    private static readonly Lazy<Color[]> ColorTable = new(() => DefineColors());

    private readonly Dictionary<int, Color> _cachedColors = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ILogItemViewModel vm)
        {
            throw new InvalidOperationException("Expected a view model of LogItemViewModel type.");
        }

        var threadHash = vm.Record.Thread.GetHashCode();

        var colorTable = ColorTable.Value;

        if (!_cachedColors.TryGetValue(threadHash, out var color))
        {
            color = colorTable[_cachedColors.Count % colorTable.Length];
            _cachedColors.Add(threadHash, color);
        }

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("This converter cannot be used in two-way binding.");
    }

    private static Color[] DefineColors()
    {
        var colorsType = typeof(Colors);
        var props = colorsType.GetProperties(BindingFlags.Static | BindingFlags.Public);
        return props.Select(x => (Color)x.GetValue(null)!)
            .Where(x => !IsDarkColor(x))
            .OrderBy(_ => Guid.NewGuid())
            .ToArray();
    }

    private static bool IsDarkColor(Color color)
    {
        return color.R * 0.2126 + color.G * 0.7152 + color.B * 0.0722 < 255 / 2;
    }
}
