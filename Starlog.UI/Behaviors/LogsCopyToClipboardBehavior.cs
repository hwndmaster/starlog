using System.Windows.Controls;
using System.Windows.Input;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class LogsCopyToClipboardBehavior : Behavior<DataGrid>
{
    protected override void OnAttached()
    {
        AssociatedObject.ClipboardCopyMode = DataGridClipboardCopyMode.None;

        AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;

        base.OnDetaching();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.C || !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            return;
        }

        var content = CopyToClipboardHelper.CreateLogsStringForClipboard(
            AssociatedObject.SelectedItems.OfType<ILogItemViewModel>());
        CopyToClipboardHelper.CopyToClipboard(content);

        e.Handled = true;
    }
}
