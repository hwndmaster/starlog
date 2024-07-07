using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class DataGridKeyBindingsBehavior : Behavior<DataGrid>
{
    protected override void OnAttached()
    {
        AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;

        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;

        base.OnDetaching();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (AssociatedObject.Items.Count == 0)
        {
            return;
        }

        var isCtrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        if (e.Key == Key.Home && !isCtrlPressed)
        {
            AssociatedObject.SelectedItem = AssociatedObject.Items[0];
            AssociatedObject.UpdateLayout();
            AssociatedObject.ScrollIntoView(AssociatedObject.SelectedItem);
            e.Handled = true;
        }
        else if (e.Key == Key.End && !isCtrlPressed)
        {
            AssociatedObject.SelectedItem = AssociatedObject.Items[^1];
            AssociatedObject.UpdateLayout();
            AssociatedObject.ScrollIntoView(AssociatedObject.SelectedItem);
            e.Handled = true;
        }
    }
}
