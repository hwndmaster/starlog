using System.Windows.Controls;
using System.Windows.Input;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views;
using MahApps.Metro.Controls;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class LogsLocateRecordBehavior : Behavior<DataGrid>
{
    private readonly MenuItem _menuItemLocate;


    public LogsLocateRecordBehavior()
    {
        _menuItemLocate = new MenuItem { Header = "Locate without filters",
            InputGestureText = "F12",
            Command = new ActionCommand(_ => LocateItem()) };
    }

    protected override void OnAttached()
    {
        AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;

        var contextMenu = XamlHelpers.EnsureDataGridRowContextMenu(AssociatedObject);
        contextMenu.Items.Add(_menuItemLocate);
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;

        var contextMenu = XamlHelpers.EnsureDataGridRowContextMenu(AssociatedObject);
        contextMenu.Items.Remove(_menuItemLocate);

        base.OnDetaching();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F12)
            return;

        LocateItem();

        e.Handled = true;
    }

    private void LocateItem()
    {
        if (AssociatedObject.DataContext is not ILogsViewModel vm)
            return;
        var item = vm.SelectedLogItems.FirstOrDefault();
        if (item is null)
            return;
        if (!vm.Filtering.CreateContext().HasAnythingSpecified
            && !vm.Search.CreateContext().HasAnythingSpecified)
        {
            return;
        }

        vm.Filtering.DropAllFilters();
        vm.Search.DropAllSearches();

        vm.SelectedLogItems.Clear();
        vm.SelectedLogItems.Add(item);
        AssociatedObject.UpdateLayout();
        AssociatedObject.Items.MoveCurrentTo(AssociatedObject.SelectedItem);
        //AssociatedObject.ScrollIntoView(AssociatedObject.SelectedItem);

        DataGridRow a;
        var scrollViewer = AssociatedObject.FindChild<ScrollViewer>().NotNull();
        var element = AssociatedObject.ItemContainerGenerator.ContainerFromItem(AssociatedObject.SelectedItem) as FrameworkElement;
        if (element is not null)
        {
            // TODO: Scrolling doesn't work yet
            Point offset = element.TransformToAncestor(scrollViewer).Transform(new Point(0, 0));
            element.TranslatePoint(new Point(0, 0), (UIElement)element.Parent)
            scrollViewer.ScrollToVerticalOffset(offset.Y);
        }
    }
}
