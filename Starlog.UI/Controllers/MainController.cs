using System.Diagnostics.CodeAnalysis;
using System.IO;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.Infrastructure.Io;
using Genius.Starlog.Core;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.UI.Controllers;

public interface IMainController
{
    /// <summary>
    ///   Performs automatic loading of a profile at startup.
    /// </summary>
    /// <returns>A task for awaiting the operation completion.</returns>
    Task AutoLoadProfileAsync();

    /// <summary>
    ///   Loads a file or folder by the specified path with an anonymous not-persisted profile.
    /// </summary>
    /// <param name="path">The path to load.</param>
    /// <param name="profileSettings">The profile settings.</param>
    /// <returns>A task for awaiting the operation completion.</returns>
    Task LoadPathAsync(string path, ProfileSettings profileSettings);

    /// <summary>
    ///   Loads a profile.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <returns>A task for awaiting the operation completion.</returns>
    Task LoadProfileAsync(Profile profile);

    void NotifyMainWindowIsLoaded();
    void NotifyProfilesAreLoaded();

    void OpenProfileContainingFolder(Profile profile);
    void SetBusy(bool isBusy);
    void ShowAddProfileForPath(string path);
    void ShowLogsTab();
    Task ShowShareViewAsync(IReadOnlyCollection<ILogItemViewModel> items);
    Task ShowAnonymousProfileLoadSettingsViewAsync(string path);

    Task Loaded { get; }
}

internal sealed class MainController : IMainController
{
    private readonly IClipboardHelper _clipboardHelper;
    private readonly ICommandBus _commandBus;
    private readonly ICurrentProfile _currentProfile;
    private readonly IDialogCoordinator _dialogCoordinator;
    private readonly IEventBus _eventBus;
    private readonly IFileService _fileService;
    private readonly ISettingsQueryService _settingsQuery;
    private readonly IUserInteraction _ui;
    private readonly Lazy<IMainViewModel> _mainViewModel;
    private readonly ILogger<MainController> _logger;

    private readonly TaskCompletionSource _profilesAreLoaded = new();
    private readonly TaskCompletionSource _mainWindowIsLoaded = new();
    private bool _anonymousProfileToBeLoaded;

    public MainController(
        IClipboardHelper clipboardHelper,
        ICommandBus commandBus,
        ICurrentProfile currentProfile,
        IDialogCoordinator dialogCoordinator,
        IEventBus eventBus,
        IFileService fileService,
        ISettingsQueryService settingsQuery,
        IUserInteraction ui,
        Lazy<IMainViewModel> mainViewModel,
        ILogger<MainController> logger)
    {
        _clipboardHelper = clipboardHelper.NotNull();
        _commandBus = commandBus.NotNull();
        _currentProfile = currentProfile.NotNull();
        _dialogCoordinator = dialogCoordinator.NotNull();
        _eventBus = eventBus.NotNull();
        _fileService = fileService.NotNull();
        _settingsQuery = settingsQuery.NotNull();
        _ui = ui.NotNull();
        _mainViewModel = mainViewModel.NotNull();
        _logger = logger.NotNull();
    }

    public async Task AutoLoadProfileAsync()
    {
        await Loaded;

        if (_anonymousProfileToBeLoaded)
        {
            return;
        }

        var settings = _settingsQuery.Get();
        if (settings.AutoLoadPreviouslyOpenedProfile && settings.AutoLoadProfile is not null)
        {
            var tab = GetProfilesTab();
            var profile = tab.Profiles.FirstOrDefault(x => x.Id == settings.AutoLoadProfile);
            profile?.LoadProfileCommand.Execute(null);
        }
    }

    public async Task LoadPathAsync(string path, ProfileSettings profileSettings)
    {
        _anonymousProfileToBeLoaded = true;
        await Loaded;

        SetBusy(true);

        await Task.Delay(10); // TODO: Helps to let the UI to show a 'busy' overlay, find a better way around.
        await Task.Run(async() =>
        {
            var profile = await _commandBus.SendAsync(new ProfileLoadAnonymousCommand(path, profileSettings));
            await _currentProfile.LoadProfileAsync(profile).ConfigureAwait(false);
            ShowLogsTab();
        })
        .ContinueWith(_ => SetBusy(false), TaskContinuationOptions.None)
        .ConfigureAwait(false);
    }

    public async Task LoadProfileAsync(Profile profile)
    {
        Guard.NotNull(profile);

        SetBusy(true);

        await Task.Delay(10); // TODO: Helps to let the UI to show a 'busy' overlay, find a better way around.
        await Task.Run(async() =>
        {
            await _currentProfile.LoadProfileAsync(profile).ConfigureAwait(false);

            if (_currentProfile.Profile is not null)
            {
                await _commandBus.SendAsync(new SettingsUpdateAutoLoadingProfileCommand(profile.Id));
                ShowLogsTab();
            }
        })
        .ContinueWith(faultedTask =>
        {
            _logger.LogError(faultedTask.Exception, faultedTask.Exception!.Message);
            _eventBus.Publish(new ProfileLoadingErrorEvent(profile, "Couldn't load profile due to errors. Check log files for details."));
        }, TaskContinuationOptions.OnlyOnFaulted)
        .ContinueWith(_ => SetBusy(false), TaskContinuationOptions.None)
        .ConfigureAwait(false);
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

    [ExcludeFromCodeCoverage]
    public void OpenProfileContainingFolder(Profile profile)
    {
        var path = _fileService.IsDirectory(profile.Path)
            ? profile.Path
            : Path.GetDirectoryName(profile.Path);
        if (path is null)
            return;
        System.Diagnostics.Process.Start("explorer.exe", path);
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
        var viewModel = new ShareLogsViewModel(_clipboardHelper, items, new ActionCommand(async _ =>
            await _dialogCoordinator.HideMetroDialogAsync(_mainViewModel.Value, customDialog)));
        customDialog.Content = new ShareLogsView { DataContext = viewModel };

        await _dialogCoordinator.ShowMetroDialogAsync(_mainViewModel.Value, customDialog);
    }

    // TODO: Cover with unit tests
    public async Task ShowAnonymousProfileLoadSettingsViewAsync(string path)
    {
        var customDialog = new CustomDialog { Title = "Set up logs" };
        var viewModelFactory = App.ServiceProvider.GetRequiredService<IViewModelFactory>(); // Cannot initialize it from ctor due to circular dependency.
        var viewModel = new AnonymousProfileLoadSettingsViewModel(viewModelFactory,
            new ActionCommand(async _ => await _dialogCoordinator.HideMetroDialogAsync(_mainViewModel.Value, customDialog)),
            new ActionCommand<ProfileSettings>(async profileSettings => await LoadPathAsync(path, profileSettings).ConfigureAwait(false)));
        customDialog.Content = new AnonymousProfileLoadSettingsView { DataContext = viewModel };
        await _dialogCoordinator.ShowMetroDialogAsync(_mainViewModel.Value, customDialog);
    }

    private IProfilesViewModel GetProfilesTab()
        => _mainViewModel.Value.Tabs.OfType<IProfilesViewModel>().First();

    public Task Loaded => Task.WhenAll(_mainWindowIsLoaded.Task, _profilesAreLoaded.Task);
}
