using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Data;
using Genius.Starlog.UI.ValueConverters;
using Genius.Starlog.UI.Views;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class LogColorizeBehavior : Behavior<DataGrid>
{
    private LogLevelToColorConverter? _logLevelToColorConverter;
    private LogFieldToColorConverter? _logFieldToColorConverter;

    protected override void OnAttached()
    {
        _logLevelToColorConverter = new LogLevelToColorConverter(AssociatedObject);
        _logFieldToColorConverter = new LogFieldToColorConverter();
        AssociatedObject.Columns.CollectionChanged += OnColumnsCollectionChanged;

        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.Columns.CollectionChanged -= OnColumnsCollectionChanged;

        base.OnDetaching();
    }

    private void OnColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add)
            return;
        var column = (DataGridColumn?)e.NewItems?[0];
        if (column is null)
            return;

        var bindingForLogLevel = new Binding(".")
        {
            Converter = _logLevelToColorConverter
        };
        var trigger1 = CreateDummyTrigger(false, bindingForLogLevel);

        var bindingForField = new MultiBinding()
        {
            Converter = _logFieldToColorConverter
        };
        bindingForField.Bindings.Add(new Binding("."));
        bindingForField.Bindings.Add(new Binding(nameof(ILogItemViewModel.ColorizeByFieldId)));
        var trigger2 = CreateDummyTrigger(true, bindingForField);

        var cellStyle = new Style(column.CellStyle.TargetType, column.CellStyle);
        cellStyle.Triggers.Add(trigger1);
        cellStyle.Triggers.Add(trigger2);
        column.CellStyle = cellStyle;

        static DataTrigger CreateDummyTrigger(bool forValue, BindingBase binding)
        {
            var trigger = new DataTrigger()
            {
                Binding = new Binding(nameof(ILogItemViewModel.ColorizeByField)),
                Value = forValue
            };
            trigger.Setters.Add(new Setter(Control.ForegroundProperty, binding));
            return trigger;
        }
    }
}
