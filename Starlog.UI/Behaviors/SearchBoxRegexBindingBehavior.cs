using System.Windows.Controls;
using System.Windows.Input;
using Genius.Starlog.UI.Views;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class SearchBoxRegexBindingBehavior : Behavior<TextBox>
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
        if (e.Key == Key.System && e.KeyboardDevice.Modifiers == ModifierKeys.Alt
            && e.SystemKey == Key.R)
        {
            if (AssociatedObject.DataContext is not ILogsViewModel vm)
                return;

            vm.Search.UseRegex = !vm.Search.UseRegex;
            e.Handled = true;
        }
    }
}
