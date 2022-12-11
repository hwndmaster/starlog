using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xaml.Behaviors;
using Genius.Atom.UI.Forms;
using Genius.Starlog.UI.Views;
using Genius.Starlog.UI.Controllers;

namespace Genius.Starlog.UI.Behaviors;

public sealed class ProfileDragDropBehavior : Behavior<DataGrid>
{
    private readonly IMainController _controller;
    private DropOverlay? _dropOverlay;

    public ProfileDragDropBehavior()
    {
        _controller = App.ServiceProvider.GetRequiredService<IMainController>();
    }

    protected override void OnAttached()
    {
        AssociatedObject.AllowDrop = true;
        AssociatedObject.Drop += OnDrop;
        AssociatedObject.DragEnter += OnDragEnter;
        AssociatedObject.DragLeave += OnDragLeave;

        _dropOverlay = new DropOverlay
        {
            Visibility = Visibility.Collapsed
        };

        var grid = WpfHelpers.FindVisualParent<Grid>(AssociatedObject).NotNull();
        StretchToGrid(_dropOverlay, grid);
        grid.Children.Add(_dropOverlay);

        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.Drop -= OnDrop;
        AssociatedObject.DragEnter -= OnDragEnter;
        AssociatedObject.DragLeave -= OnDragLeave;

        var grid = WpfHelpers.FindVisualParent<Grid>(AssociatedObject);
        grid.NotNull().Children.Remove(_dropOverlay);
        _dropOverlay = null;

        base.OnDetaching();
    }

    public void OnDragEnter(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        _dropOverlay!.Visibility = Visibility.Visible;
        e.Effects = DragDropEffects.Link;
        e.Handled = true;
    }

    public void OnDragLeave(object sender, DragEventArgs e)
    {
        _dropOverlay!.Visibility = Visibility.Collapsed;
    }

    public void OnDrop(object sender, DragEventArgs e)
    {
        var fileDrop = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (fileDrop is null || fileDrop.Length == 0)
        {
            return;
        }

        _controller.ShowAddProfileForPath(fileDrop[0]);

        _dropOverlay!.Visibility = Visibility.Collapsed;
    }

    private static void StretchToGrid(UIElement element, Grid grid)
    {
        element.SetValue(Grid.RowProperty, 0);
        element.SetValue(Grid.RowSpanProperty, Math.Max(1, grid.RowDefinitions.Count));
        element.SetValue(Grid.ColumnProperty, 0);
        element.SetValue(Grid.ColumnSpanProperty, Math.Max(1, grid.ColumnDefinitions.Count));
    }
}
