using System.Windows.Controls;
using System.Windows.Input;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.UI.Views;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class ProfileContextMenuBehavior : Behavior<DataGrid>
{
    protected override void OnAttached()
    {
        var contextMenu = WpfHelpers.EnsureDataGridRowContextMenu(AssociatedObject);

        contextMenu.Items.Add(new MenuItem
        {
            Header = "Load",
            InputGestureText = "Enter",
            Command = new ActionCommand(_ => LoadProfile())
        });
        contextMenu.Items.Add(new MenuItem
        {
            Header = "Modify",
            InputGestureText = "F4",
            Command = new ActionCommand(_ => ModifySelected())
        });
        contextMenu.Items.Add(new MenuItem
        {
            Header = "Duplicate",
            Command = new ActionCommand(_ => DuplicateProfile())
        });
        contextMenu.Items.Add(new MenuItem
        {
            Header = "Delete",
            InputGestureText = "Del",
            Command = new ActionCommand(_ => DeleteSelected())
        });

        AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;

        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;

        base.OnDetaching();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            DeleteSelected();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            LoadProfile();
            e.Handled = true;
        }
        else if (e.Key == Key.F4)
        {
            ModifySelected();
            e.Handled = true;
        }
    }

    private void DeleteSelected()
    {
        if (AssociatedObject.DataContext is not IProfilesViewModel vm)
            return;

        vm.DeleteProfileCommand.Execute(null);
    }

    private void DuplicateProfile()
    {
        if (AssociatedObject.DataContext is not IProfilesViewModel vm)
            return;

        var selectedProfile = vm.Profiles.FirstOrDefault(x => x.IsSelected);
        if (selectedProfile is null)
            return;

        vm.IsAddEditProfileVisible = false;
        vm.OpenAddProfileFlyoutCommand.Execute(null);
        var editingProfile = vm.EditingProfile.NotNull();
        editingProfile.CopyFrom(selectedProfile, " Copy");
    }

    private void LoadProfile()
    {
        if (AssociatedObject.DataContext is not IProfilesViewModel vm)
            return;

        var profile = vm.Profiles.FirstOrDefault(x => x.IsSelected);
        profile?.LoadProfileCommand.Execute(null);
    }

    private void ModifySelected()
    {
        if (AssociatedObject.DataContext is not IProfilesViewModel vm)
            return;

        vm.OpenEditProfileFlyoutCommand.Execute(null);
    }
}
