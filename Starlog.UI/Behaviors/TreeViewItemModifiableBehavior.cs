using System.Windows.Controls;
using System.Windows.Input;
using Genius.Atom.UI.Forms;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class TreeViewItemModifiableBehavior : Behavior<FrameworkElement>
{
    private TreeViewItem? _treeViewItem;
    private IHasModifyCommand? _vmWithModifySupport;
    private IHasDeleteCommand? _vmWithDeleteSupport;

    protected override void OnAttached()
    {
        _treeViewItem = AssociatedObject.FindVisualParent<TreeViewItem>().NotNull();
        var vm = _treeViewItem.DataContext;
        _vmWithModifySupport = vm as IHasModifyCommand;
        _vmWithDeleteSupport = vm as IHasDeleteCommand;

        InitializeContextMenu();

        _treeViewItem.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
        _treeViewItem.AddHandler(UIElement.MouseDownEvent, new MouseButtonEventHandler(OnMouseDown), true);
        _treeViewItem.AddHandler(TreeViewItem.SelectedEvent, new RoutedEventHandler(OnTreeViewItemSelected), true);

        base.OnAttached();
    }

    private void OnTreeViewItemSelected(object sender, RoutedEventArgs e)
    {
        Console.Write(_treeViewItem?.IsSelected);
    }

    protected override void OnDetaching()
    {
        _treeViewItem?.RemoveHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown));
        _treeViewItem?.RemoveHandler(UIElement.MouseDownEvent, new MouseButtonEventHandler(OnMouseDown));
        _treeViewItem?.RemoveHandler(TreeViewItem.SelectedEvent, new RoutedEventHandler(OnTreeViewItemSelected));

        base.OnDetaching();
    }

    private void InitializeContextMenu()
    {
        if (_vmWithModifySupport is null && _vmWithDeleteSupport is null)
        {
            return;
        }

        _treeViewItem!.ContextMenu = new ContextMenu();

        if (_vmWithModifySupport is not null)
        {
            _treeViewItem.ContextMenu.Items.Add(new MenuItem
            {
                Header = "Modify",
                Command = new ActionCommand(_ => _vmWithModifySupport!.ModifyCommand.Execute(null))
            });
        }

        if (_vmWithDeleteSupport is not null)
        {
            _treeViewItem.ContextMenu.Items.Add(new MenuItem
            {
                Header = "Delete",
                Command = new ActionCommand(_ => _vmWithDeleteSupport!.DeleteCommand.Execute(null))
            });
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (_vmWithDeleteSupport is not null && e.Key == Key.Delete)
        {
            _vmWithDeleteSupport.DeleteCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_vmWithModifySupport is not null
            && e.LeftButton == MouseButtonState.Pressed
            && e.ClickCount == 2)
        {
            _vmWithModifySupport.ModifyCommand.Execute(null);
            e.Handled = true;
        }
    }
}
