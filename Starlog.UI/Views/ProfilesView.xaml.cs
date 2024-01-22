using System.Diagnostics.CodeAnalysis;
using Genius.Atom.UI.Forms.Wpf;

namespace Genius.Starlog.UI.Views;

[ExcludeFromCodeCoverage]
public partial class ProfilesView
{
    public ProfilesView()
    {
        InitializeComponent();

        this.WhenLoadedOneTime().Subscribe(_ =>
        {
            WpfHelpers.AddFlyout<AddEditProfileFlyout>(this, nameof(ProfilesViewModel.IsAddEditProfileVisible), nameof(ProfilesViewModel.EditingProfile));
        });
    }
}
