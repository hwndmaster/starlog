using System.Reactive.Linq;
using System.Windows.Controls;
using Genius.Atom.UI.Forms.Wpf;

namespace Genius.Starlog.UI.Helpers;

public static class DataGridNavigation
{
    public static IDisposable ScrollToItem(DataGrid dataGrid, int itemIndex, int itemsCount)
    {
        var verticalOffset = itemIndex == itemsCount - 1
            ? itemIndex // Scroll to it if it is last
            : Math.Max(0, itemIndex - 1); // Scroll to previous item or at 0
        var scrollViewer = dataGrid.FindVisualChildren<ScrollViewer>().First();

        return Observable.FromEventPattern<EventHandler, EventArgs>(
            h => dataGrid.LayoutUpdated += h, h => dataGrid.LayoutUpdated -= h)

            // Only scroll when the DataGrid got fully reloaded.
            .Where(_ => dataGrid.Items.Count == itemsCount)
            .Take(1)
            .SubscribeOnUiThread(_ =>
            {
                scrollViewer.ScrollToVerticalOffset(verticalOffset);
            });
    }
}
