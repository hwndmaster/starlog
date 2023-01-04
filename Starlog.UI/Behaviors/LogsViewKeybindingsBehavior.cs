using System.Windows.Controls;
using System.Windows.Input;
using Genius.Starlog.UI.Views;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class LogsViewKeybindingsBehavior : Behavior<LogsView>
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
        if (e.Key == Key.F && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            if (AssociatedObject.FindName("SearchBox") is not TextBox searchBox)
            {
                return;
            }

            searchBox.Focus();

            e.Handled = true;
        }
    }
}
