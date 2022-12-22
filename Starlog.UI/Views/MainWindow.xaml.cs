using MahApps.Metro.Controls;

namespace Genius.Starlog.UI.Views;

public partial class MainWindow : MetroWindow
{
    public MainWindow(IMainViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}
