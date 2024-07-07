using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class DataGridAutoScrollBehavior : Behavior<DataGrid>
{
    public static readonly DependencyProperty AutoScrollProperty = DependencyProperty.RegisterAttached(
        "AutoScroll", typeof(bool), typeof(DataGridAutoScrollBehavior), new PropertyMetadata(default(bool)));

    private IDisposable? _subscription;

    public static void SetAutoScroll(DependencyObject element, bool value)
        => element.NotNull().SetValue(AutoScrollProperty, value);

    public static bool GetAutoScroll(DependencyObject element)
        => (bool)element.NotNull().GetValue(AutoScrollProperty);

    public bool AutoScroll
    {
        get { return (bool)GetValue(AutoScrollProperty); }
        set { SetValue(AutoScrollProperty, value); }
    }

    protected override void OnAttached()
    {
        var dpd = DependencyPropertyDescriptor.FromProperty(Atom.UI.Forms.Controls.AutoGrid.Properties.ItemsSourceProperty, typeof(DataGrid));
        dpd?.AddValueChanged(AssociatedObject, OnDataGridItemsSourceChanged);

        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        var dpd = DependencyPropertyDescriptor.FromProperty(Atom.UI.Forms.Controls.AutoGrid.Properties.ItemsSourceProperty, typeof(DataGrid));
        dpd?.RemoveValueChanged(AssociatedObject, OnDataGridItemsSourceChanged);

        _subscription?.Dispose();

        base.OnDetaching();
    }

    private void OnDataGridItemsSourceChanged(object? sender, EventArgs e)
    {
        var itemsSource = Atom.UI.Forms.Controls.AutoGrid.Properties.GetItemsSource(AssociatedObject) as INotifyCollectionChanged;
        _subscription?.Dispose();
        _subscription = itemsSource?.WhenCollectionChanged()
            .Where(x => x.Action == NotifyCollectionChangedAction.Add
                && AutoScroll)
            .Subscribe(_ =>
            {
                if (AssociatedObject.Items.Count == 0)
                    return;
                AssociatedObject.ScrollIntoView(AssociatedObject.Items[^1]);
            });
        }
}
