using System.Windows.Controls;
using Genius.Starlog.UI.Views;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class ProfileContextMenuBehavior : Behavior<DataGrid>
{
    protected override void OnAttached()
    {
        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(new MenuItem
        {
            Header = "Modify profile",
            Command = new ActionCommand(_ => {
                if (AssociatedObject.DataContext is not IProfilesViewModel vm) return;
                vm.OpenEditProfileFlyoutCommand.Execute(null);
            })
        });

        var rowStyle = new Style(typeof(DataGridRow), AssociatedObject.RowStyle);
        rowStyle.Setters.Add(new Setter(FrameworkElement.ContextMenuProperty, contextMenu));
        AssociatedObject.RowStyle = rowStyle;

        base.OnAttached();
    }
}
