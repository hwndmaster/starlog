using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.ViewModels;

namespace Genius.Starlog.UI.Controllers;

public interface IMainController
{
    void ShowLogsForActiveProfile();
}

internal sealed class MainController : IMainController
{
    private readonly Lazy<IMainViewModel> _mainViewModel;

    public MainController(Lazy<IMainViewModel> mainViewModel)
    {
        _mainViewModel = mainViewModel.NotNull();
    }

    public void ShowLogsForActiveProfile()
    {
        var tab = _mainViewModel.Value.Tabs.OfType<ILogsViewModel>().First();
        _mainViewModel.Value.SelectedTabIndex = _mainViewModel.Value.Tabs.IndexOf(tab);

        tab.LoadCurrentProfile();
    }
}
