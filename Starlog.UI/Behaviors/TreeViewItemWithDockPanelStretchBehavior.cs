using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class TreeViewItemWithDockPanelStretchBehavior : Behavior<DockPanel>
{
    private TreeViewItem? _treeViewItem;
    private double? _offset;

    protected override void OnAttached()
    {
        AssociatedObject.LastChildFill = false;

        _treeViewItem = AssociatedObject.FindVisualParent<TreeViewItem>().NotNull();
        _treeViewItem.SizeChanged += OnSizeChanged;

        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        if (_treeViewItem is not null)
            _treeViewItem.SizeChanged -= OnSizeChanged;
        base.OnDetaching();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateWidth();

    private void UpdateWidth()
    {
        if (_treeViewItem is null) return;

        _offset ??= _treeViewItem.DesiredSize.Width - AssociatedObject.DesiredSize.Width;
        AssociatedObject.Width = _treeViewItem.ActualWidth - _offset.Value;
    }
}
