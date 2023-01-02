using System.Windows.Controls;
using System.Windows.Input;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.UI.Views;
using Genius.Starlog.UI.Views.LogSearchAndFiltering;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class TreeViewItemBookmarkableBehavior : Behavior<FrameworkElement>
{
    private TreeViewItem? _treeViewItem;
    private ILogsViewModel? _logsVm;

    protected override void OnAttached()
    {
        _treeViewItem = AssociatedObject.FindVisualParent<TreeViewItem>().NotNull();
        var logsView = AssociatedObject.FindVisualParent<LogsView>().NotNull();
        _logsVm = logsView.DataContext as ILogsViewModel;

        if (_logsVm is null)
            return;

        if (_treeViewItem.DataContext is not LogFilterBookmarkedCategoryViewModel)
            return;

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
        if (_logsVm is null)
        {
            return;
        }

        _treeViewItem!.ContextMenu ??= new ContextMenu();

        _treeViewItem.ContextMenu.Items.Add(new MenuItem
        {
            Header = "Remove all bookmarks",
            Command = new ActionCommand(_ => _logsVm.UnBookmarkAll())
        });
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (_logsVm is not null && (e.Key == Key.Delete || e.Key == Key.Escape))
        {
            _logsVm.UnBookmarkAll();

            e.Handled = true;
        }
    }
}
