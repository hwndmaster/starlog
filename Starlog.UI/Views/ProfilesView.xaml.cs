using System.Diagnostics.CodeAnalysis;
using Genius.Atom.UI.Forms.Wpf;

namespace Genius.Starlog.UI.Views;

[ExcludeFromCodeCoverage]
public partial class ProfilesView
{
    public ProfilesView()
    {
        InitializeComponent();

        this.Loaded += (sender, args) =>
            WpfHelpers.AddFlyout<AddEditProfileFlyout>(this, nameof(ProfilesViewModel.IsAddEditProfileVisible), nameof(ProfilesViewModel.EditingProfile));
    }
}
