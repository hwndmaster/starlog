using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.UI.Views;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class LogsBookmarkableBehavior : Behavior<DataGrid>
{
    private readonly MenuItem _menuItemBookmark;

    public LogsBookmarkableBehavior()
    {
        _menuItemBookmark = new MenuItem { Header = "Bookmark",
            InputGestureText = "F8",
            Command = new ActionCommand(_ => BookmarkItem()) };
    }

    protected override void OnAttached()
    {
        AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;

        var contextMenu = WpfHelpers.EnsureDataGridRowContextMenu(AssociatedObject);
        contextMenu.Items.Add(_menuItemBookmark);

        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;

        var contextMenu = WpfHelpers.EnsureDataGridRowContextMenu(AssociatedObject);
        contextMenu.Items.Remove(_menuItemBookmark);

        base.OnDetaching();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F8)
        {
            BookmarkItem();
            e.Handled = true;
        }
    }

    private void BookmarkItem()
    {
        if (AssociatedObject.DataContext is not ILogsViewModel vm)
            return;
        var firstSelected = vm.SelectedLogItems.FirstOrDefault();
        if (firstSelected is null)
            return;

        var newValue = !firstSelected.IsBookmarked;

        foreach (var item in vm.SelectedLogItems)
        {
            item.IsBookmarked = newValue;
        }
    }
}
