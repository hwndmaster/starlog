using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Genius.Starlog.Core;
using Genius.Starlog.Core.Configuration;
using Genius.Starlog.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Genius.Starlog.UI.ValueConverters;

public sealed class LogLevelToColorConverter : IValueConverter
{
    internal static readonly Color ColorForMinor = Colors.SlateGray;
    internal static readonly Color ColorForWarning = Colors.Gold;
    internal static readonly Color ColorForMajor = Colors.IndianRed;
    internal static readonly Color ColorForCritical = Colors.Red;

    private static Lazy<LogLevelMappingConfiguration> _configLazy = new(() =>
        App.ServiceProvider.GetRequiredService<IOptions<LogLevelMappingConfiguration>>().Value);
    private static Dictionary<int /* Log Level Id */, Color> _cache = new();

    private readonly Color _standardColor;

    static LogLevelToColorConverter()
    {
        App.ServiceProvider.GetRequiredService<ICurrentProfile>()
            .ProfileClosed
            .Subscribe(_ => _cache.Clear());
    }

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

        if (!_cache.TryGetValue(vm.Record.Level.Id, out var color))
        {
            if (_configLazy.Value.TreatAsMinor.Contains(vm.Record.Level.Name, StringComparer.InvariantCultureIgnoreCase))
                color = ColorForMinor;
            else if (_configLazy.Value.TreatAsWarning.Contains(vm.Record.Level.Name, StringComparer.InvariantCultureIgnoreCase))
                color = ColorForWarning;
            else if (_configLazy.Value.TreatAsError.Contains(vm.Record.Level.Name, StringComparer.InvariantCultureIgnoreCase))
                color = ColorForMajor;
            else if (_configLazy.Value.TreatAsCritical.Contains(vm.Record.Level.Name, StringComparer.InvariantCultureIgnoreCase))
                color = ColorForCritical;
            else
                color = _standardColor;

            _cache.Add(vm.Record.Level.Id, color);
        }

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("This converter cannot be used in two-way binding.");
    }
}
