using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.UI.Views;
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

        var contextMenu = WpfHelpers.EnsureDataGridRowContextMenu(AssociatedObject);
        contextMenu.Items.Add(_menuItemLocate);

        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;

        var contextMenu = WpfHelpers.EnsureDataGridRowContextMenu(AssociatedObject);
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

        vm.SelectedLogItems.Clear();

        vm.Filtering.DropAllFilters();
        vm.Search.DropAllSearches();
        vm.ResetGrouping();

        Observable.FromEventPattern<EventHandler, EventArgs>(
            h => AssociatedObject.LayoutUpdated += h, h => AssociatedObject.LayoutUpdated -= h)
            .Take(1)
            .Subscribe(_ =>
            {
                Dispatcher.Invoke(() => {
                    AssociatedObject.Items.MoveCurrentTo(AssociatedObject.SelectedItem);
                    AssociatedObject.ScrollIntoView(AssociatedObject.SelectedItem);
                }, DispatcherPriority.ContextIdle);
            });

        vm.SelectedLogItems.Add(item);

        /* UNDONE:
        var dispatcher = App.ServiceProvider.GetRequiredService<IUiDispatcher>();

        dispatcher.BeginInvoke(() => {
            AssociatedObject.UpdateLayout();
            ForceUIToUpdate();
            AssociatedObject.Items.MoveCurrentTo(AssociatedObject.SelectedItem);
            AssociatedObject.ScrollIntoView(AssociatedObject.SelectedItem);

            var scrollViewer = AssociatedObject.FindChild<ScrollViewer>().NotNull();
            var element = AssociatedObject.ItemContainerGenerator.ContainerFromItem(AssociatedObject.SelectedItem) as FrameworkElement;
            if (element is not null)
            {
                Point offset = element.TransformToAncestor(scrollViewer).Transform(new Point(0, 0));
                //var offset = element.TranslatePoint(new Point(0, 0), (UIElement)element.Parent);
                scrollViewer.ScrollToVerticalOffset(offset.Y);
            }
        });

        public static void ForceUIToUpdate()
        {
            DispatcherFrame frame = new DispatcherFrame();

            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(delegate(object parameter)
            {
                frame.Continue = false;
                return null;
            }), null);

            Dispatcher.PushFrame(frame);
        }
        */
    }
}
