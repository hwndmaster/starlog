using System.Windows.Controls;
using System.Windows.Input;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class LogsCopyToClipboardBehavior : Behavior<DataGrid>
{
    private readonly MenuItem _menuItemCopy;
    private readonly MenuItem _menuItemCopyWhole;
    private readonly MenuItem _menuItemCopyMsg;

    private DataGridClipboardCopyMode _previousClipboardCopyMode;

    public LogsCopyToClipboardBehavior()
    {
        _menuItemCopyWhole = new MenuItem { Header = "Everything",
            InputGestureText = "Ctrl+C",
            Command = new ActionCommand(_ => CopyToClipboard()) };
        _menuItemCopyMsg = new MenuItem { Header = "Message(s)", Command = new ActionCommand(_ =>
        {
            var content = CopyToClipboardHelper.CreateLogMessagesStringForClipboard(AssociatedObject.SelectedItems.OfType<ILogItemViewModel>());
            CopyToClipboardHelper.CopyToClipboard(content);
        }) };
        _menuItemCopy = new MenuItem { Header = "Copy to clipboard", Items = {
            _menuItemCopyWhole,
            _menuItemCopyMsg
        }};
    }

    protected override void OnAttached()
    {
        _previousClipboardCopyMode = AssociatedObject.ClipboardCopyMode;
        AssociatedObject.ClipboardCopyMode = DataGridClipboardCopyMode.None;

        AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;

        var contextMenu = XamlHelpers.EnsureDataGridRowContextMenu(AssociatedObject);
        contextMenu.Items.Add(_menuItemCopy);
    }

    protected override void OnDetaching()
    {
        AssociatedObject.ClipboardCopyMode = _previousClipboardCopyMode;
        AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;

        var contextMenu = XamlHelpers.EnsureDataGridRowContextMenu(AssociatedObject);
        contextMenu.Items.Remove(_menuItemCopy);

        base.OnDetaching();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.C || !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            return;
        }

        CopyToClipboard();

        e.Handled = true;
    }

    private void CopyToClipboard()
    {
        var content = CopyToClipboardHelper.CreateLogsStringForClipboard(
            AssociatedObject.SelectedItems.OfType<ILogItemViewModel>());
        CopyToClipboardHelper.CopyToClipboard(content);
    }
}
