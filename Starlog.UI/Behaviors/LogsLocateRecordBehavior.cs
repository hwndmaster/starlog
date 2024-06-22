using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class LogsLocateRecordBehavior : Behavior<DataGrid>, IDisposable
{
    private IDisposable? _subscription;
    private readonly MenuItem _menuItemLocate;


    public LogsLocateRecordBehavior()
    {
        _menuItemLocate = new MenuItem { Header = "Locate without filters",
            InputGestureText = "F12",
            Command = new ActionCommand(_ => LocateItem()) };
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }

    protected override void OnAttached()
    {
        AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;

        var contextMenu = WpfHelpers.EnsureDataGridRowContextMenu(AssociatedObject);
        contextMenu.Items.Add(_menuItemLocate);

        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;

        var contextMenu = WpfHelpers.EnsureDataGridRowContextMenu(AssociatedObject);
        contextMenu.Items.Remove(_menuItemLocate);

        Dispose();

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

        vm.SelectedLogItems.Clear();

        vm.Filtering.DropAllFilters();
        vm.Search.DropAllSearches();
        vm.ResetGrouping();

        vm.SelectedLogItems.Add(item);

        var selectedItemIndex = vm.LogItems.IndexOf(item);

        DataGridNavigation.ScrollToItem(AssociatedObject, selectedItemIndex, vm.LogItems.Count);
    }
}
