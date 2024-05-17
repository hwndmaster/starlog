using System.Windows.Controls;
using System.Windows.Input;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class LogsCopyToClipboardBehavior : Behavior<DataGrid>
{
    private readonly IClipboardHelper _clipboardHelper;

    private readonly MenuItem _menuItemCopy;

    private DataGridClipboardCopyMode _previousClipboardCopyMode;

    public LogsCopyToClipboardBehavior()
    {
        _clipboardHelper = App.ServiceProvider.GetRequiredService<IClipboardHelper>();

        var menuItemCopyWhole = new MenuItem { Header = "Everything",
            InputGestureText = "Ctrl+C",
            Command = new ActionCommand(_ => CopyToClipboard()) };
        var menuItemCopyMsg = new MenuItem { Header = "Message(s)", Command = new ActionCommand(_ =>
        {
            var content = _clipboardHelper.CreateLogMessagesStringForClipboard(AssociatedObject.SelectedItems.OfType<ILogItemViewModel>());
            _clipboardHelper.CopyToClipboard(content);
        }) };
        _menuItemCopy = new MenuItem { Header = "Copy to clipboard", Items = {
            menuItemCopyWhole,
            menuItemCopyMsg
        }};
    }

    protected override void OnAttached()
    {
        _previousClipboardCopyMode = AssociatedObject.ClipboardCopyMode;
        AssociatedObject.ClipboardCopyMode = DataGridClipboardCopyMode.None;

        AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;

        var contextMenu = WpfHelpers.EnsureDataGridRowContextMenu(AssociatedObject);
        contextMenu.Items.Add(_menuItemCopy);
    }

    protected override void OnDetaching()
    {
        AssociatedObject.ClipboardCopyMode = _previousClipboardCopyMode;
        AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;

        var contextMenu = WpfHelpers.EnsureDataGridRowContextMenu(AssociatedObject);
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
        var content = _clipboardHelper.CreateLogsStringForClipboard(
            AssociatedObject.SelectedItems.OfType<ILogItemViewModel>());
        _clipboardHelper.CopyToClipboard(content);
    }
}
