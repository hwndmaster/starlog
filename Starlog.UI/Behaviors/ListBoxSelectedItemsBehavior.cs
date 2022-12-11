using System.Collections;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class ListBoxSelectedItemsBehavior : Behavior<ListBox>
{
    public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
        nameof(SelectedItems),
        typeof(IList),
        typeof(ListBoxSelectedItemsBehavior),
        new PropertyMetadata(OnSelectedItemsChanged));
    private bool _isUpdating = false;

    public IList SelectedItems
    {
        get { return (IList)GetValue(SelectedItemsProperty); }
        set { SetValue(SelectedItemsProperty, value); }
    }

    protected override void OnAttached()
    {
        AssociatedObject.SelectionChanged += OnListBoxSelectionChanged;

        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.SelectionChanged -= OnListBoxSelectionChanged;

        base.OnDetaching();
    }

    private void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating)
            return;

        foreach (var item in e.AddedItems)
        {
            SelectedItems.Add(item);
        }

        foreach (var item in e.RemovedItems)
        {
            SelectedItems.Remove(item);
        }
    }

    private static void OnSelectedItemsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        if (args.NewValue is not IList list)
        {
            return;
        }

        var behavior = (ListBoxSelectedItemsBehavior)obj;
        behavior._isUpdating = true;
        try
        {
            behavior.AssociatedObject.SelectedItems.Clear();
            foreach (var item in list)
            {
                behavior.AssociatedObject.SelectedItems.Add(item);
            }
        }
        finally
        {
            behavior._isUpdating = false;
        }
    }
}
