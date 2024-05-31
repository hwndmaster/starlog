using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Media;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.ValueConverters;

public sealed class LogFieldToColorConverter : IMultiValueConverter
{
    private static readonly Lazy<Color[]> _colorTable = new(() => DefineColors());
    private readonly Dictionary<int, Color> _cachedColors = [];

    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is null || values.Length == 0)
            return null;
        if (values[0] is not ILogItemViewModel vm)
            throw new InvalidOperationException("Expected a view model of LogItemViewModel type.");
        if (vm.ColorizeByFieldId is null || vm.ColorizeByFieldId.Value >= vm.Record.FieldValueIndices.Length)
            return null;

        var fieldValueId = vm.Record.FieldValueIndices[vm.ColorizeByFieldId.Value];
        var colorTable = _colorTable.Value;

        if (!_cachedColors.TryGetValue(fieldValueId, out var color))
        {
            color = colorTable[_cachedColors.Count % colorTable.Length];
            _cachedColors.Add(fieldValueId, color);
        }

        return new SolidColorBrush(color);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
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
