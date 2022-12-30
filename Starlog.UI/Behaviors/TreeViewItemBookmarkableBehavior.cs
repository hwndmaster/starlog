using System.Windows.Controls;
using System.Windows.Input;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.UI.Views;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class TreeViewItemBookmarkableBehavior : Behavior<FrameworkElement>
{
    private TreeViewItem? _treeViewItem;
    private ILogsViewModel? _vm;

    protected override void OnAttached()
    {
        _treeViewItem = AssociatedObject.FindVisualParent<TreeViewItem>().NotNull();
        var logsView = AssociatedObject.FindVisualParent<LogsView>().NotNull();
        _vm = logsView.DataContext as ILogsViewModel;

        if (_vm is null)
        {
            return;
        }

        InitializeContextMenu();

        _treeViewItem.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);

        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        _treeViewItem?.RemoveHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown));

        base.OnDetaching();
    }

    private void InitializeContextMenu()
    {
        if (_vm is null)
        {
            return;
        }

        _treeViewItem!.ContextMenu ??= new ContextMenu();

        _treeViewItem.ContextMenu.Items.Add(new MenuItem
        {
            Header = "Remove all bookmarks",
            Command = new ActionCommand(_ => _vm.UnBookmarkAll())
        });
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (_vm is not null && (e.Key == Key.Delete || e.Key == Key.Escape))
        {
            _vm.UnBookmarkAll();
            e.Handled = true;
        }
    }
}
