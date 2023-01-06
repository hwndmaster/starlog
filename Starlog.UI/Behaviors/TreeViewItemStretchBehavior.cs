using System.Windows.Controls;
using Genius.Atom.UI.Forms.Wpf;
using MahApps.Metro.Controls;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class TreeViewItemStretchBehavior : Behavior<DockPanel>
{
    protected override void OnAttached()
    {
        AssociatedObject.LastChildFill = false;

        var treeViewItem = AssociatedObject.FindVisualParent<TreeViewItem>().NotNull();

        var grid = treeViewItem.FindChild<Grid>();
        if (grid?.ColumnDefinitions.Count == 3)
        {
            grid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            grid.ColumnDefinitions[2].Width = new GridLength(0, GridUnitType.Pixel);
            grid.ColumnDefinitions[2].MaxWidth = 0;
        }

        base.OnAttached();
    }
}
