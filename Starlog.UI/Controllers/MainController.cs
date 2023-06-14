using System.IO;
using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Console;
using Genius.Starlog.UI.Views;
using MahApps.Metro.Controls.Dialogs;
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
    /// <param name="options">The options of an anonymous profile.</param>
    /// <returns>A task for awaiting the operation completion.</returns>
    Task LoadPathAsync(LoadPathCommandLineOptions options);

    void NotifyMainWindowIsLoaded();
    void NotifyProfilesAreLoaded();

    void SetBusy(bool isBusy);
    void ShowAddProfileForPath(string path);
    void ShowLogsTab();
    Task ShowShareViewAsync(IReadOnlyCollection<ILogItemViewModel> items);

    Task Loaded { get; }
}

internal sealed class MainController : IMainController
{
    private readonly ICommandBus _commandBus;
    private readonly IDialogCoordinator _dialogCoordinator;
    private readonly ILogContainer _logContainer;
    private readonly ILogCodecContainer _logCodecContainer;
    private readonly ISettingsQueryService _settingsQuery;
    private readonly IProfileSettingsTemplateQueryService _templatesQuery;
    private readonly IUserInteraction _ui;
    private readonly Lazy<IMainViewModel> _mainViewModel;
    private readonly ILogger<MainController> _logger;

    private readonly TaskCompletionSource _profilesAreLoaded = new();
    private readonly TaskCompletionSource _mainWindowIsLoaded = new();
    private bool _anonymousProfileToBeLoaded;

    public MainController(
        ICommandBus commandBus,
        IDialogCoordinator dialogCoordinator,
        ILogContainer logContainer,
        ILogCodecContainer logCodecContainer,
        ISettingsQueryService settingsQuery,
        IProfileSettingsTemplateQueryService templatesQuery,
        IUserInteraction ui,
        Lazy<IMainViewModel> mainViewModel,
        ILogger<MainController> logger)
    {
        _commandBus = commandBus.NotNull();
        _dialogCoordinator = dialogCoordinator.NotNull();
        _logContainer = logContainer.NotNull();
        _logCodecContainer = logCodecContainer.NotNull();
        _settingsQuery = settingsQuery.NotNull();
        _templatesQuery = templatesQuery.NotNull();
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

    public async Task LoadPathAsync(LoadPathCommandLineOptions options)
    {
        _anonymousProfileToBeLoaded = true;
        await Loaded;

        SetBusy(true);

        await Task.Delay(10); // TODO: Helps to let the UI to show a 'busy' overlay, find a better way around.
        await Task.Run(async() =>
        {
            ProfileSettings? settings = null;
            if (!string.IsNullOrEmpty(options.Template))
            {
                // TODO: Cover this condition with unit tests
                ProfileSettingsTemplate? template = null;
                if (Guid.TryParse(options.Template, out var templateId))
                {
                    template = await _templatesQuery.FindByIdAsync(templateId);
                }
                if (template is null)
                {
                    template = (await _templatesQuery.GetAllAsync()).FirstOrDefault(x => x.Name.Equals(options.Template, StringComparison.OrdinalIgnoreCase));
                }
                if (template is not null)
                {
                    settings = template.Settings;
                }
            }

            if (settings is null)
            {
                var codecName = options.Codec ?? "Plain Text";
                var logCodec = _logCodecContainer.GetLogCodecs().FirstOrDefault(x => x.Name.Equals(codecName, StringComparison.OrdinalIgnoreCase));
                if (logCodec is null)
                {
                    _logger.LogWarning("Couldn't load a profile with unknown codec '{codec}'.", codecName);
                    return;
                }
                var profileLogCodec = _logCodecContainer.CreateProfileLogCodec(logCodec);
                if (profileLogCodec is null)
                {
                    // TODO: Cover this condition with unit tests
                    _logger.LogWarning("Couldn't create profile codec settings for codec '{codec}'.", codecName);
                    return;
                }
                if (options.CodecSettings is not null)
                {
                    var processor = _logCodecContainer.CreateLogCodecProcessor(profileLogCodec);
                    if (!processor.ReadFromCommandLineArguments(profileLogCodec, options.CodecSettings.ToArray()))
                    {
                        // Couldn't read arguments, terminating...
                        _logger.LogWarning("Couldn't load a profile from '{path}' with codec '{codec}' and the following settings: {settings}", options.Path, codecName, string.Join(',', options.CodecSettings));
                        return;
                    }
                }

                settings = new ProfileSettings
                {
                    LogCodec = profileLogCodec,
                    FileArtifactLinesCount = options.FileArtifactLinesCount ?? 0
                };
            }

            var profile = await _commandBus.SendAsync(new ProfileLoadAnonymousCommand(options.Path, settings));
            await _logContainer.LoadProfileAsync(profile).ConfigureAwait(false);
            ShowLogsTab();
        })
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

    // TODO: To cover with unit tests
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

    private IProfilesViewModel GetProfilesTab()
        => _mainViewModel.Value.Tabs.OfType<IProfilesViewModel>().First();

    public Task Loaded => Task.WhenAll(_mainWindowIsLoaded.Task, _profilesAreLoaded.Task);
}
