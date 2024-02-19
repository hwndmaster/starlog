using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Views;
using Genius.Starlog.UI.Views.ProfileSettings;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.UI.Controllers;

public interface IProfileLoadingController
{
    /// <summary>
    ///   Performs automatic loading of a profile at startup.
    /// </summary>
    /// <returns>A task for awaiting the operation completion.</returns>
    Task AutoLoadProfileAsync();

    /// <summary>
    ///   Loads an anonymous not-persisted profile.
    /// </summary>
    /// <param name="profileSettings">The profile settings.</param>
    /// <returns>A task for awaiting the operation completion.</returns>
    Task LoadProfileSettingsAsync(ProfileSettingsBase profileSettings);

    /// <summary>
    ///   Loads a profile.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <returns>A task for awaiting the operation completion.</returns>
    Task LoadProfileAsync(Profile profile);

    Task ShowAnonymousProfileLoadSettingsViewAsync(string path);
}

internal sealed class ProfileLoadingController : IProfileLoadingController
{
    private readonly ICommandBus _commandBus;
    private readonly ICurrentProfile _currentProfile;
    private readonly IDialogCoordinator _dialogCoordinator;
    private readonly IEventBus _eventBus;
    private readonly IMainController _mainController;
    private readonly Lazy<IMainViewModel> _mainViewModel;
    private readonly ILogger<ProfileLoadingController> _logger;
    private readonly ISettingsQueryService _settingsQuery;
    private readonly IProfileSettingsViewModelFactory _profileSettingsViewModelFactory;

    private bool _anonymousProfileToBeLoaded;

    public ProfileLoadingController(
        ICommandBus commandBus,
        ICurrentProfile currentProfile,
        IDialogCoordinator dialogCoordinator,
        IEventBus eventBus,
        ILogger<ProfileLoadingController> logger,
        IMainController mainController,
        Lazy<IMainViewModel> mainViewModel,
        ISettingsQueryService settingsQuery,
        IProfileSettingsViewModelFactory profileSettingsViewModelFactory)
    {
        _commandBus = commandBus.NotNull();
        _currentProfile = currentProfile.NotNull();
        _dialogCoordinator = dialogCoordinator.NotNull();
        _eventBus = eventBus.NotNull();
        _logger = logger.NotNull();
        _mainController = mainController.NotNull();
        _mainViewModel = mainViewModel.NotNull();
        _settingsQuery = settingsQuery.NotNull();
        _profileSettingsViewModelFactory = profileSettingsViewModelFactory.NotNull();
    }

    public async Task AutoLoadProfileAsync()
    {
        await _mainController.Loaded;

        if (_anonymousProfileToBeLoaded)
        {
            return;
        }

        var settings = _settingsQuery.Get();
        if (settings.AutoLoadPreviouslyOpenedProfile && settings.AutoLoadProfile is not null)
        {
            var tab = _mainController.GetProfilesTab();
            var profile = tab.Profiles.FirstOrDefault(x => x.Id == settings.AutoLoadProfile);
            profile?.LoadProfileCommand.Execute(null);
        }
    }

    public async Task LoadProfileSettingsAsync(ProfileSettingsBase profileSettings)
    {
        _anonymousProfileToBeLoaded = true;
        await _mainController.Loaded;

        _mainController.SetBusy(true);

        await Task.Delay(10); // TODO: Helps to let the UI to show a 'busy' overlay, find a better way around.
        await Task.Run(async() =>
        {
            var profile = await _commandBus.SendAsync(new ProfileLoadAnonymousCommand(profileSettings));
            await _currentProfile.LoadProfileAsync(profile).ConfigureAwait(false);
            _mainController.ShowLogsTab();
        })
        .ContinueWith(_ => _mainController.SetBusy(false), TaskContinuationOptions.None)
        .ConfigureAwait(false);
    }

    public async Task LoadProfileAsync(Profile profile)
    {
        Guard.NotNull(profile);

        _mainController.SetBusy(true);

        await Task.Delay(10); // TODO: Helps to let the UI to show a 'busy' overlay, find a better way around.
        await Task.Run(async() =>
        {
            await _currentProfile.LoadProfileAsync(profile).ConfigureAwait(false);

            if (_currentProfile.Profile is not null)
            {
                await _commandBus.SendAsync(new SettingsUpdateAutoLoadingProfileCommand(profile.Id));
                _mainController.ShowLogsTab();
            }
        })
        .ContinueWith(faultedTask =>
        {
            _logger.LogError(faultedTask.Exception, faultedTask.Exception!.Message);
            _eventBus.Publish(new ProfileLoadingErrorEvent(profile, "Couldn't load profile due to errors. Check log files for details."));
        }, TaskContinuationOptions.OnlyOnFaulted)
        .ContinueWith(_ => _mainController.SetBusy(false), TaskContinuationOptions.None)
        .ConfigureAwait(false);
    }

    public async Task ShowAnonymousProfileLoadSettingsViewAsync(string path)
    {
        var customDialog = new CustomDialog { Title = "Set up logs" };
        var viewModel = _profileSettingsViewModelFactory.CreateAnonymousProfileLoadSettings(
            path,
            new ActionCommand(async _ => await _dialogCoordinator.HideMetroDialogAsync(_mainViewModel.Value, customDialog)),
            new ActionCommand<ProfileSettingsBase>(LoadProfileSettingsAsync));
        customDialog.Content = new AnonymousProfileLoadSettingsView { DataContext = viewModel };
        await _dialogCoordinator.ShowMetroDialogAsync(_mainViewModel.Value, customDialog);
    }
}
