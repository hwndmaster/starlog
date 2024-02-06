using Genius.Atom.UI.Forms.Wpf;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace Genius.Starlog.UI.Views;

public partial class MainWindow : MetroWindow
{
    public MainWindow(IMainViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        DialogParticipation.SetRegister(this, viewModel);

        this.WhenLoadedOneTime().Subscribe(_ =>
        {
            WpfHelpers.AddFlyout<ErrorsFlyout>(this, nameof(MainViewModel.IsErrorsFlyoutVisible), nameof(MainViewModel.Errors));
        });
    }
}
