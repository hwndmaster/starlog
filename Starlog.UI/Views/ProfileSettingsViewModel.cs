using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.UI.Forms.Validation;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Views.ProfileLogCodecs;
using ReactiveUI;

namespace Genius.Starlog.UI.Views;

public interface IProfileSettingsViewModel
{
    ProfileSettings? CommitChanges();
    void CopyFrom(IProfileSettingsViewModel source);
    void ResetForm(ProfileSettings? profileSettings = null);
}

// TODO: Cover with unit tests
public sealed class ProfileSettingsViewModel : ViewModelBase, IProfileSettingsViewModel
{
    private readonly IProfileSettingsTemplateQueryService _templatesQuery;
    private readonly IUiDispatcher _dispatcher;
    private readonly IUserInteraction _ui;
    private ProfileSettings _profileSettings;

    public ProfileSettingsViewModel(
        ProfileSettings profileSettings,
        IEventBus eventBus,
        IProfileSettingsTemplateQueryService templatesQuery,
        ILogCodecContainer logCodecContainer,
        IViewModelFactory vmFactory,
        IUiDispatcher dispatcher,
        IUserInteraction ui)
    {
        // Dependencies:
        Guard.NotNull(logCodecContainer);
        Guard.NotNull(vmFactory);
        _dispatcher = dispatcher.NotNull();
        _profileSettings = profileSettings.NotNull();
        _templatesQuery = templatesQuery.NotNull();
        _ui = ui.NotNull();

        // Members initialization:
        foreach (var logCodec in logCodecContainer.GetLogCodecs())
        {
            LogCodecs.Add(vmFactory.CreateLogCodec(logCodec, profileSettings.LogCodec));
        }
        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(LogsLookupPattern)));
        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(DateTimeFormat)));

        InitializeProperties(() =>
        {
            ResetForm();
        });

        // Actions:
        ResetCommand = new ActionCommand(_ => ResetForm());
        ApplyTemplateCommand = new ActionCommand(arg =>
        {
            if (arg is ProfileSettings settings)
            {
                LogCodec = LogCodecs.First(x => x.ProfileLogCodec.LogCodec.Id == settings.LogCodec.LogCodec.Id);
                var dummyVm = vmFactory.CreateLogCodec(settings.LogCodec.LogCodec, settings.LogCodec);
                LogCodec.CopySettingsFrom(dummyVm);
                FileArtifactLinesCount = settings.FileArtifactLinesCount;
                LogsLookupPattern = settings.LogsLookupPattern;
                DateTimeFormat = settings.DateTimeFormat;
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

    public ProfileSettings? CommitChanges()
    {
        Validate();
        LogCodec.Validate();

        if (HasErrors || LogCodec.HasErrors)
        {
            _ui.ShowWarning("Cannot proceed while there are errors in the form.");
            return null;
        }

        _profileSettings = new ProfileSettings
        {
            LogCodec = LogCodec.ProfileLogCodec,
            FileArtifactLinesCount = FileArtifactLinesCount,
            LogsLookupPattern = LogsLookupPattern,
            DateTimeFormat = DateTimeFormat
        };

        return _profileSettings;
    }

    public void CopyFrom(IProfileSettingsViewModel source)
    {
        if (source is not ProfileSettingsViewModel sourceClass)
            return;

        LogCodec = LogCodecs.First(x => x.ProfileLogCodec.LogCodec.Id == sourceClass.LogCodec.ProfileLogCodec.LogCodec.Id);
        LogCodec.CopySettingsFrom(sourceClass.LogCodec);
        FileArtifactLinesCount = sourceClass.FileArtifactLinesCount;
        LogsLookupPattern = sourceClass.LogsLookupPattern;
        DateTimeFormat = sourceClass.DateTimeFormat;
    }

    private async Task ReloadTemplatesAsync()
    {
        var templates = await _templatesQuery.GetAllAsync();
        await _dispatcher.BeginInvoke(() =>
            Templates.ReplaceItems(templates.Select(x => new ProfileSettingsTemplateSelectionViewModel(x))));
    }

    public void ResetForm(ProfileSettings? profileSettings = null)
    {
        _profileSettings = profileSettings ?? _profileSettings;

        LogCodec = LogCodecs.First(x => x.ProfileLogCodec.LogCodec.Id == _profileSettings.LogCodec.LogCodec.Id);
        FileArtifactLinesCount = _profileSettings.FileArtifactLinesCount;
        LogsLookupPattern = _profileSettings.LogsLookupPattern ?? ProfileSettings.DefaultLogsLookupPattern;
        DateTimeFormat = _profileSettings.DateTimeFormat ?? ProfileSettings.DefaultDateTimeFormat;
    }

    public DelayedObservableCollection<LogCodecViewModel> LogCodecs { get; }
        = new TypedObservableCollection<LogCodecViewModel, LogCodecViewModel>();

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

    public string LogsLookupPattern
    {
        get => GetOrDefault(ProfileSettings.DefaultLogsLookupPattern);
        set => RaiseAndSetIfChanged(value);
    }

    public string DateTimeFormat
    {
        get => GetOrDefault(string.Empty);
        set => RaiseAndSetIfChanged(value);
    }

    public bool AnyTemplateAvailable
    {
        get => GetOrDefault<bool>();
        set => RaiseAndSetIfChanged(value);
    }

    public ObservableCollection<ProfileSettingsTemplateSelectionViewModel> Templates { get; } = new();

    public IActionCommand ResetCommand { get; }
    public IActionCommand ApplyTemplateCommand { get; }
}
