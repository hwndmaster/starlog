using System.Windows.Controls;
using System.Windows.Data;
using Genius.Starlog.UI.ValueConverters;
using Genius.Starlog.UI.Views;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class LogColorizeBehavior : Behavior<DataGrid>
{
    private IDisposable? _subscription;

    protected override void OnAttached()
    {
        AssociatedObject.AutoGeneratedColumns += OnAutoGeneratedColumns;

        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.AutoGeneratedColumns -= OnAutoGeneratedColumns;
        _subscription?.Dispose();
        _subscription = null;

        base.OnDetaching();
    }

    private void OnAutoGeneratedColumns(object? sender, EventArgs args)
    {
        var bindingForLogLevel = new Binding(".")
        {
            Converter = new LogLevelToColorConverter(AssociatedObject)
        };
        var trigger1 = CreateDummyTrigger(false, bindingForLogLevel);

        var bindingForLogThread = new Binding(".")
        {
            Converter = new LogThreadToColorConverter()
        };
        var trigger2 = CreateDummyTrigger(true, bindingForLogThread);

        foreach (var column in AssociatedObject.Columns)
        {
            column.CellStyle.Triggers.Add(trigger1);
            column.CellStyle.Triggers.Add(trigger2);
        }

        static DataTrigger CreateDummyTrigger(bool forValue, Binding binding)
        {
            var trigger = new DataTrigger()
            {
                Binding = new Binding(nameof(ILogItemViewModel.ColorizeByThread)),
                Value = forValue
            };
            trigger.Setters.Add(new Setter(Control.ForegroundProperty, binding));
            return trigger;
        }
    }
}
