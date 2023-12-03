using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.UI.Forms.Validation;
using Genius.Starlog.Core;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Controllers;
using Genius.Starlog.UI.Views.ProfileLogCodecs;
using ReactiveUI;

namespace Genius.Starlog.UI.Views;

public interface IProfileViewModel : ISelectable
{
    void CopyFrom(IProfileViewModel source, string? nameSuffix = null);

    Guid? Id { get; }
    Profile? Profile { get; }
    string Name { get; set; }
    string Path { get; set; }
    IActionCommand CommitProfileCommand { get; }
    IActionCommand LoadProfileCommand { get; }
}

// TODO: Cover with unit tests
public sealed class ProfileViewModel : ViewModelBase, IProfileViewModel
{
    private readonly ICommandBus _commandBus;
    private readonly IProfileQueryService _profileQuery;
    private readonly IProfileSettingsTemplateQueryService _templatesQuery;
    private readonly IMainController _controller;
    private readonly ICurrentProfile _currentProfile;
    private readonly ILogCodecContainer _logCodecContainer;
    private readonly IViewModelFactory _vmFactory;
    private readonly IUiDispatcher _dispatcher;
    private readonly IUserInteraction _ui;

    private Profile? _profile;

    public ProfileViewModel(
        Profile? profile,
        ICommandBus commandBus,
        ICurrentProfile currentProfile,
        IEventBus eventBus,
        IMainController controller,
        IProfileQueryService profileQuery,
        IProfileSettingsTemplateQueryService templatesQuery,
        ILogCodecContainer logCodecContainer,
        IViewModelFactory vmFactory,
        IUiDispatcher dispatcher,
        IUserInteraction ui)
    {
        // Dependencies:
        _commandBus = commandBus.NotNull();
        _controller = controller.NotNull();
        _currentProfile = currentProfile.NotNull();
        _dispatcher = dispatcher.NotNull();
        _logCodecContainer = logCodecContainer.NotNull();
        _profileQuery = profileQuery.NotNull();
        _templatesQuery = templatesQuery.NotNull();
        _vmFactory = vmFactory.NotNull();
        _ui = ui.NotNull();

        // Members initialization:
        _profile = profile;

        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(Name)));
        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(Path)));
        AddValidationRule(new PathExistsValidationRule(nameof(Path)));

        foreach (var logCodec in _logCodecContainer.GetLogCodecs())
        {
            LogCodecs.Add(_vmFactory.CreateLogCodec(logCodec, _profile?.Settings.LogCodec));
        }

        InitializeProperties(() =>
        {
            if (_profile is not null)
            {
                ResetForm();
                Reconcile();
            }
        });

        // Actions:
        CommitProfileCommand = new ActionCommand(_ => CommitProfile());
        LoadProfileCommand = new ActionCommand(async _ => {
            _controller.SetBusy(true);
            await Task.Delay(10); // TODO: Helps to let the UI to show a 'busy' overlay, find a better way around.
            await Task.Run(async() =>
            {
                Guard.NotNull(_profile);
                await _currentProfile.LoadProfileAsync(_profile).ConfigureAwait(false);

                if (_currentProfile.Profile is not null)
                {
                    await _commandBus.SendAsync(new SettingsUpdateAutoLoadingProfileCommand(_profile.Id));
                    _controller.ShowLogsTab();
                }
            })
            .ContinueWith(_ => _controller.SetBusy(false), TaskContinuationOptions.None)
            .ConfigureAwait(false);
        });
        ResetCommand = new ActionCommand(_ => ResetForm(), _ => _profile is not null);
        ApplyTemplateCommand = new ActionCommand(arg =>
        {
            if (arg is ProfileSettings settings)
            {
                LogCodec = LogCodecs.First(x => x.ProfileLogCodec.LogCodec.Id == settings.LogCodec.LogCodec.Id);
                var dummyVm = _vmFactory.CreateLogCodec(settings.LogCodec.LogCodec, settings.LogCodec);
                LogCodec.CopySettingsFrom(dummyVm);
                FileArtifactLinesCount = settings.FileArtifactLinesCount;
            }
        });

        // Subscriptions:
        eventBus.WhenFired<ProfileSettingsTemplatesAffectedEvent>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async _ => await ReloadTemplatesAsync());
        Templates.WhenCollectionChanged().Subscribe(_ =>
            AnyTemplateAvailable = Templates.Any());

        Task.Run(ReloadTemplatesAsync);
    }

    public void CopyFrom(IProfileViewModel source, string? nameSuffix = null)
    {
        if (source is not ProfileViewModel sourceProfile)
            return;

        Name = sourceProfile.Name + (nameSuffix ?? string.Empty);
        Path = sourceProfile.Path;
        LogCodec = LogCodecs.First(x => x.ProfileLogCodec.LogCodec.Id == sourceProfile.LogCodec.ProfileLogCodec.LogCodec.Id);
        LogCodec.CopySettingsFrom(sourceProfile.LogCodec);
        FileArtifactLinesCount = sourceProfile.FileArtifactLinesCount;
    }

    public void Reconcile()
    {
        if (_profile is null)
        {
            return;
        }

        Name = _profile.Name;
        Path = _profile.Path;
        LogCodec = LogCodecs.First(x => x.ProfileLogCodec.LogCodec.Id == _profile.Settings.LogCodec.LogCodec.Id);
        FileArtifactLinesCount = _profile.Settings.FileArtifactLinesCount;
    }

    private async Task<bool> CommitProfile()
    {
        Validate();
        LogCodec.Validate();

        if (HasErrors || LogCodec.HasErrors)
        {
            _ui.ShowWarning("Cannot proceed while there are errors in the form.");
            return false;
        }

        if (_profile is null)
        {
            var profileId = await _commandBus.SendAsync(new ProfileCreateCommand
            {
                Name = Name,
                Path = Path,
                Settings = new ProfileSettings
                {
                    LogCodec = LogCodec.ProfileLogCodec,
                    FileArtifactLinesCount = FileArtifactLinesCount
                }
            });
            _profile = await _profileQuery.FindByIdAsync(profileId);
        }
        else
        {
            await _commandBus.SendAsync(new ProfileUpdateCommand(_profile.Id)
            {
                Name = Name,
                Path = Path,
                Settings = new ProfileSettings
                {
                    LogCodec = LogCodec.ProfileLogCodec,
                    FileArtifactLinesCount = FileArtifactLinesCount
                }
            });
        }

        return true;
    }

    private async Task ReloadTemplatesAsync()
    {
        var templates = await _templatesQuery.GetAllAsync();
        await _dispatcher.BeginInvoke(() =>
            Templates.ReplaceItems(templates.Select(x => new ProfileSettingsTemplateSelectionViewModel(x))));
    }

    private void ResetForm()
    {
        Name = _profile?.Name ?? Name;
        Path = _profile?.Path ?? Path;
        LogCodec = _profile is null
            ? LogCodec
            : LogCodecs.First(x => x.ProfileLogCodec.LogCodec.Id == _profile.Settings.LogCodec.LogCodec.Id);
        FileArtifactLinesCount = _profile?.Settings.FileArtifactLinesCount ?? FileArtifactLinesCount;
    }

    public DelayedObservableCollection<LogCodecViewModel> LogCodecs { get; }
        = new TypedObservableCollection<LogCodecViewModel, LogCodecViewModel>();

    public Guid? Id => _profile?.Id;
    public Profile? Profile => _profile;

    public string PageTitle => _profile is null ? "Add profile" : "Edit profile";

    public string Name
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public string Path
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public LogCodecViewModel LogCodec
    {
        get => GetOrDefault(LogCodecs[0]);
        set => RaiseAndSetIfChanged(value);
    }

    public int FileArtifactLinesCount
    {
        get => GetOrDefault(0);
        set => RaiseAndSetIfChanged(value);
    }

    public bool IsSelected
    {
        get => GetOrDefault<bool>();
        set => RaiseAndSetIfChanged(value);
    }

    public bool AnyTemplateAvailable
    {
        get => GetOrDefault<bool>();
        set => RaiseAndSetIfChanged(value);
    }

    public ObservableCollection<ProfileSettingsTemplateSelectionViewModel> Templates { get; } = new();

    public IActionCommand CommitProfileCommand { get; }

    public IActionCommand LoadProfileCommand { get; }

    public IActionCommand ResetCommand { get; }
    public IActionCommand ApplyTemplateCommand { get; }
}
