using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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

    // TODO: Consider moving this logic to behaviors
    private void Filter_KeyUp(object sender, KeyEventArgs e)
    {
        var filterTextbox = (TextBox)sender;

        if (e.Key == Key.Enter || e.Key == Key.Escape)
        {
            if (e.Key == Key.Escape)
            {
                filterTextbox.Text = string.Empty;
            }

            var bindingExpr = BindingOperations.GetBindingExpression(filterTextbox, TextBox.TextProperty);
            bindingExpr?.UpdateSource();
        }
    }
}
