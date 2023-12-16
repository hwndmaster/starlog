using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xaml.Behaviors;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.UI.Controllers;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.Behaviors;

public sealed class ProfileDragDropBehavior : Behavior<DataGrid>
{
    #region IsDraggingProperty
    public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.RegisterAttached(
        "IsDragging", typeof(bool), typeof(ProfileDragDropBehavior), new PropertyMetadata(default(bool)));
    public static void SetIsDragging(DependencyObject element, bool value)
        => element.SetValue(IsDraggingProperty, value);
    public static bool GetIsDragging(DependencyObject element)
        => (bool)element.GetValue(IsDraggingProperty);
    #endregion

    private readonly IMainController _controller;
    private ProfilesViewDropOverlay? _dropOverlay;

    public ProfileDragDropBehavior()
    {
        _controller = App.ServiceProvider.GetRequiredService<IMainController>();
    }

    protected override void OnAttached()
    {
        AssociatedObject.AllowDrop = true;
        AssociatedObject.DragEnter += OnDragEnter;

        _dropOverlay = new ProfilesViewDropOverlay
        {
            Visibility = Visibility.Collapsed
        };
        _dropOverlay.DragEnter += OnOverlayDragEnter;
        _dropOverlay.DragLeave += OnOverlayDragLeave;

        var grid = AssociatedObject.FindVisualParent<Grid>().NotNull();
        StretchToGrid(_dropOverlay, grid);
        grid.Children.Add(_dropOverlay);

        var createProfileArea = (UIElement)_dropOverlay.FindName("CreateProfile");
        createProfileArea.AllowDrop = true;
        createProfileArea.Drop += OnCreateProfileDrop;
        createProfileArea.DragEnter += (_, __) => SetIsDragging(createProfileArea, true);
        createProfileArea.DragLeave += (_, __) => SetIsDragging(createProfileArea, false);

        var openImmediatelyArea = (UIElement)_dropOverlay.FindName("OpenImmediately");
        openImmediatelyArea.AllowDrop = true;
        openImmediatelyArea.Drop += OnOpenImmediatelyDrop;
        openImmediatelyArea.DragEnter += (_, __) => SetIsDragging(openImmediatelyArea, true);
        openImmediatelyArea.DragLeave += (_, __) => SetIsDragging(openImmediatelyArea, false);

        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.DragEnter -= OnDragEnter;

        var grid = AssociatedObject.FindVisualParent<Grid>().NotNull();
        grid.Children.Remove(_dropOverlay);
        _dropOverlay = null;

        base.OnDetaching();
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        _dropOverlay!.Visibility = Visibility.Visible;
    }

    private void OnOverlayDragEnter(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        e.Effects = DragDropEffects.Link;
        e.Handled = true;
    }

    private void OnOverlayDragLeave(object sender, DragEventArgs e)
    {
        _dropOverlay!.Visibility = Visibility.Collapsed;
    }

    private void OnCreateProfileDrop(object sender, DragEventArgs e)
    {
        var fileDrop = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (fileDrop is null || fileDrop.Length == 0)
        {
            return;
        }

        _controller.ShowAddProfileForPath(fileDrop[0]);

        _dropOverlay!.Visibility = Visibility.Collapsed;
    }

    private void OnOpenImmediatelyDrop(object sender, DragEventArgs e)
    {
        var fileDrop = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (fileDrop is null || fileDrop.Length == 0)
        {
            return;
        }

        _controller.ShowAnonymousProfileLoadSettingsViewAsync(fileDrop[0]);

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
