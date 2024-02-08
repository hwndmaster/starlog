using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using ReactiveUI;

namespace Genius.Starlog.UI.Views.ProfileSettings;

public interface IProfileSettingsViewModel
{
    ProfileSettingsBase? CommitChanges();
    void CopyFrom(IProfileSettingsViewModel source);
    void ResetForm();
    void SelectPlainTextForPath(string path);
    string Source { get; }
}

// TODO: Cover with unit tests
public sealed class ProfileSettingsViewModel : ViewModelBase, IProfileSettingsViewModel
{
    private readonly IProfileSettingsTemplateQueryService _templatesQuery;
    private readonly IUiDispatcher _dispatcher;
    private readonly IUserInteraction _ui;

    public ProfileSettingsViewModel(
        ProfileSettingsBase profileSettings,
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
        _templatesQuery = templatesQuery.NotNull();
        _ui = ui.NotNull();

        // Members initialization:
        foreach (var logCodec in logCodecContainer.GetLogCodecs())
        {
            ProfileSettings.Add(vmFactory.CreateLogCodec(logCodec, profileSettings));
        }

        InitializeProperties(() =>
        {
            ResetForm();
        });

        // Actions:
        ResetCommand = new ActionCommand(_ => ResetForm());
        ApplyTemplateCommand = new ActionCommand(arg =>
        {
            if (arg is not ProfileSettingsBase settings)
                return; // Unexpected

            SelectedProfileSettings = ProfileSettings.First(x => x.ProfileSettings.LogCodec.Id == settings.LogCodec.Id);
            var dummyVm = vmFactory.CreateLogCodec(settings.LogCodec, settings);
            SelectedProfileSettings.CopySettingsFrom(dummyVm);
        });

        // Subscriptions:
        eventBus.WhenFired<ProfileSettingsTemplatesAffectedEvent>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async _ => await ReloadTemplatesAsync());
        Templates.WhenCollectionChanged().Subscribe(_ =>
            AnyTemplateAvailable = Templates.Any());

        Task.Run(ReloadTemplatesAsync);
    }

    public ProfileSettingsBase? CommitChanges()
    {
        Validate();
        if (HasErrors)
        {
            _ui.ShowWarning(StringResources.ValidationError);
            return null;
        }

        if (!SelectedProfileSettings.CommitChanges())
        {
            _ui.ShowWarning(StringResources.ValidationError);
            return null;
        }

        return SelectedProfileSettings.ProfileSettings;
    }

    public void CopyFrom(IProfileSettingsViewModel source)
    {
        if (source is not ProfileSettingsViewModel sourceClass)
            return;

        SelectedProfileSettings = ProfileSettings.First(x => x.ProfileSettings.LogCodec.Id == sourceClass.SelectedProfileSettings.ProfileSettings.LogCodec.Id);
        SelectedProfileSettings.CopySettingsFrom(sourceClass.SelectedProfileSettings);
    }

    public void SelectPlainTextForPath(string path)
    {
        SelectedProfileSettings = ProfileSettings.First(x => x.ProfileSettings is PlainTextProfileSettings);
        ((PlainTextProfileSettingsViewModel)SelectedProfileSettings).Path = path;
    }

    private async Task ReloadTemplatesAsync()
    {
        var templates = await _templatesQuery.GetAllAsync();
        await _dispatcher.BeginInvoke(() =>
            Templates.ReplaceItems(templates.Select(x => new ProfileSettingsTemplateSelectionViewModel(x))));
    }

    public void ResetForm()
    {
        SelectedProfileSettings.ResetForm();
    }

    public string Source => SelectedProfileSettings.ProfileSettings.Source;

    public DelayedObservableCollection<ProfileSettingsBaseViewModel> ProfileSettings { get; }
        = new TypedObservableCollection<ProfileSettingsBaseViewModel, ProfileSettingsBaseViewModel>();

    public ProfileSettingsBaseViewModel SelectedProfileSettings
    {
        get => GetOrDefault(ProfileSettings[0]);
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
