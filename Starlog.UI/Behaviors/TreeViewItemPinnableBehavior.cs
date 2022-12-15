using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class TreeViewItemPinnableBehavior : Behavior<FrameworkElement>
{
    private TreeViewItem? _treeViewItem;
    private IHasPinnedFlag? _vm;

    protected override void OnAttached()
    {
        _treeViewItem = AssociatedObject.FindVisualParent<TreeViewItem>().NotNull();
        _vm = _treeViewItem.DataContext as IHasPinnedFlag;

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
            Header = "Pin/Unpin",
            Command = new ActionCommand(_ => _vm!.IsPinned = !_vm!.IsPinned)
        });
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (_vm is not null && e.Key == Key.Space)
        {
            _vm.IsPinned = !_vm.IsPinned;
            e.Handled = true;
        }
    }
}
