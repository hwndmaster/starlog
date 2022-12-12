using System.IO;
using Genius.Starlog.UI.ViewModels;

namespace Genius.Starlog.UI.Controllers;

public interface IMainController
{
    void SetBusy(bool isBusy);
    void ShowAddProfileForPath(string path);
    void ShowLogsTab();
}

internal sealed class MainController : IMainController
{
    private readonly Lazy<IMainViewModel> _mainViewModel;

    public MainController(Lazy<IMainViewModel> mainViewModel)
    {
        _mainViewModel = mainViewModel.NotNull();
    }

    public void SetBusy(bool isBusy)
    {
        _mainViewModel.Value.IsBusy = isBusy;
    }

    public void ShowAddProfileForPath(string path)
    {
        var tab = _mainViewModel.Value.Tabs.OfType<IProfilesViewModel>().First();
        tab.IsAddEditProfileVisible = false;
        tab.OpenAddProfileFlyoutCommand.Execute(null);
        tab.EditingProfile!.Name = Path.GetFileNameWithoutExtension(path);
        tab.EditingProfile!.Path = path;
    }

    public void ShowLogsTab()
    {
        var tab = _mainViewModel.Value.Tabs.OfType<ILogsViewModel>().First();
        _mainViewModel.Value.SelectedTabIndex = _mainViewModel.Value.Tabs.IndexOf(tab);
    }
}
