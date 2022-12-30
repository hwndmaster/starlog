using System.IO;
using Genius.Starlog.UI.Views;
using MahApps.Metro.Controls.Dialogs;

namespace Genius.Starlog.UI.Controllers;

public interface IMainController
{
    void SetBusy(bool isBusy);
    void ShowAddProfileForPath(string path);
    void ShowLogsTab();
    Task ShowShareViewAsync(IReadOnlyCollection<ILogItemViewModel> items);
}

internal sealed class MainController : IMainController
{
    private readonly IDialogCoordinator _dialogCoordinator;
    private readonly IUserInteraction _ui;
    private readonly Lazy<IMainViewModel> _mainViewModel;

    public MainController(IDialogCoordinator dialogCoordinator,
        IUserInteraction ui,
        Lazy<IMainViewModel> mainViewModel)
    {
        _dialogCoordinator = dialogCoordinator.NotNull();
        _ui = ui.NotNull();
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

    public async Task ShowShareViewAsync(IReadOnlyCollection<ILogItemViewModel> items)
    {
        if (items.Count == 0)
        {
            _ui.ShowInformation("No logs were selected to share.");
            return;
        }

        var customDialog = new CustomDialog { Title = "Share logs" };
        var viewModel = new ShareLogsViewModel(items, new ActionCommand(async _ =>
            await _dialogCoordinator.HideMetroDialogAsync(_mainViewModel.Value, customDialog)));
        customDialog.Content = new ShareLogsView { DataContext = viewModel };

        await _dialogCoordinator.ShowMetroDialogAsync(_mainViewModel.Value, customDialog);
    }
}
