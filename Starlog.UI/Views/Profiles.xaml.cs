using System.Diagnostics.CodeAnalysis;
using Genius.Atom.UI.Forms;
using Genius.Starlog.UI.ViewModels;

namespace Genius.Starlog.UI.Views;

[ExcludeFromCodeCoverage]
public partial class Profiles
{
    public Profiles()
    {
        InitializeComponent();

        this.Loaded += (sender, args) =>
            WpfHelpers.AddFlyout<AddEditProfileFlyout>(this, nameof(ProfilesViewModel.IsAddEditProfileVisible), nameof(ProfilesViewModel.EditingProfile));
    }
}
