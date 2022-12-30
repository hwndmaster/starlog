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
    }
}
