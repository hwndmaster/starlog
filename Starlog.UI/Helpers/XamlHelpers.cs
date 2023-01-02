using System.Windows.Controls;

namespace Genius.Starlog.UI.Helpers;

public static class XamlHelpers
{
    public static ContextMenu EnsureDataGridRowContextMenu(DataGrid dataGrid)
    {
        if (dataGrid.RowStyle?.Setters
            .OfType<Setter>()
            .FirstOrDefault(x => x.Property == FrameworkElement.ContextMenuProperty)
            ?.Value is not ContextMenu contextMenu)
        {
            contextMenu = new ContextMenu();
            var rowStyle = new Style(typeof(DataGridRow), dataGrid.RowStyle);
            rowStyle.Setters.Add(new Setter(FrameworkElement.ContextMenuProperty, contextMenu));
            dataGrid.RowStyle = rowStyle;
        }

        return contextMenu;
    }
}
