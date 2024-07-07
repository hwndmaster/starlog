using System.Diagnostics;
using System.IO;
using Genius.Atom.Infrastructure.Io;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views;
using MahApps.Metro.Controls.Dialogs;

namespace Genius.Starlog.UI.Controllers;

public interface IMainController
{
    IProfilesViewModel GetProfilesTab();

    void NotifyMainWindowIsLoaded();
    void NotifyProfilesAreLoaded();

    void Locate(Profile profile);
    void SetBusy(bool isBusy);
    void ShowAddProfileForPath(string path);
    void ShowLogsTab();
    Task ShowShareViewAsync(IReadOnlyCollection<ILogItemViewModel> items);

    Task Loaded { get; }
}

internal sealed class MainController : IMainController
{
    private readonly IClipboardHelper _clipboardHelper;
    private readonly IDialogCoordinator _dialogCoordinator;
    private readonly IFileService _fileService;
    private readonly IUserInteraction _ui;
    private readonly Lazy<IMainViewModel> _mainViewModel;

    private readonly TaskCompletionSource _profilesAreLoaded = new();
    private readonly TaskCompletionSource _mainWindowIsLoaded = new();

    public MainController(
        IClipboardHelper clipboardHelper,
        IDialogCoordinator dialogCoordinator,
        IFileService fileService,
        IUserInteraction ui,
        Lazy<IMainViewModel> mainViewModel)
    {
        _clipboardHelper = clipboardHelper.NotNull();
        _dialogCoordinator = dialogCoordinator.NotNull();
        _fileService = fileService.NotNull();
        _ui = ui.NotNull();
        _mainViewModel = mainViewModel.NotNull();
    }

    public void NotifyMainWindowIsLoaded()
    {
        // Shouldn't be called more than once from the app.
        _mainWindowIsLoaded.SetResult();
    }

    public void NotifyProfilesAreLoaded()
    {
        _profilesAreLoaded.TrySetResult();
    }

    public void Locate(Profile profile)
    {
        // TODO: Cover cases with unit tests, including `else`
        if (profile.Settings is PlainTextProfileSettings plainTextProfileSettings)
        {
            var path = _fileService.IsDirectory(plainTextProfileSettings.Path)
                ? plainTextProfileSettings.Path
                : Path.GetDirectoryName(plainTextProfileSettings.Path);
            if (path is null)
                return;
            Process.Start("explorer.exe", path);
        }
        else if (profile.Settings is XmlProfileSettings xmlProfileSettings)
        {
            throw new NotImplementedException();
        }
        else if (profile.Settings is WindowsEventProfileSettings windowsEventProfileSettings)
        {
            var source = windowsEventProfileSettings.Sources.FirstOrDefault();
            var eventViewerApp = Path.Combine(Environment.SystemDirectory, "eventvwr.exe");
            var psi = new ProcessStartInfo(eventViewerApp)
            {
                UseShellExecute = true,
                Arguments = source is null ? string.Empty : "/c:" + source
            };
            Process.Start(psi);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public void SetBusy(bool isBusy)
    {
        _mainViewModel.Value.IsBusy = isBusy;
    }

    // TODO: To cover with unit tests
    public void ShowAddProfileForPath(string path)
    {
        var tab = GetProfilesTab();
        tab.IsAddEditProfileVisible = false;
        tab.OpenAddProfileFlyoutCommand.Execute(null);
        tab.EditingProfile!.Name = Path.GetFileNameWithoutExtension(path);
        tab.EditingProfile!.ProfileSettings.SelectPlainTextForPath(path);
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
        var viewModel = new ShareLogsViewModel(_clipboardHelper, items, new ActionCommand(async _ =>
            await _dialogCoordinator.HideMetroDialogAsync(_mainViewModel.Value, customDialog)));
        customDialog.Content = new ShareLogsView { DataContext = viewModel };

        await _dialogCoordinator.ShowMetroDialogAsync(_mainViewModel.Value, customDialog);
    }

    public IProfilesViewModel GetProfilesTab()
        => _mainViewModel.Value.Tabs.OfType<IProfilesViewModel>().First();

    public Task Loaded => Task.WhenAll(_mainWindowIsLoaded.Task, _profilesAreLoaded.Task);
}
