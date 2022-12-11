using System.Diagnostics.CodeAnalysis;
using Genius.Atom.UI.Forms;
using Genius.Starlog.UI.ViewModels;

namespace Genius.Starlog.UI.Views;

[ExcludeFromCodeCoverage]
public partial class Logs
{
    public Logs()
    {
        InitializeComponent();

        this.Loaded += (sender, args) =>
            WpfHelpers.AddFlyout<AddEditProfileFilterFlyout>(this, nameof(LogsViewModel.IsAddEditProfileFilterVisible), nameof(LogsViewModel.EditingProfileFilter));
    }
}
