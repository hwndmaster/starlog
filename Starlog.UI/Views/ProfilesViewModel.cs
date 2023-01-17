using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Starlog.Core;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.AutoGridBuilders;

namespace Genius.Starlog.UI.Views;

public interface IProfilesViewModel : ITabViewModel, IDisposable
{
    ObservableCollection<IProfileViewModel> Profiles { get; }
    bool IsAddEditProfileVisible { get; set; }
    IProfileViewModel? EditingProfile { get; }

    ICommand OpenAddProfileFlyoutCommand { get; }
    ICommand OpenEditProfileFlyoutCommand { get; }
    ICommand DeleteProfileCommand { get; }
}

internal sealed class ProfilesViewModel : TabViewModelBase, IProfilesViewModel
{
    private readonly IProfileQueryService _profileQuery;
    private readonly IViewModelFactory _vmFactory;
    private readonly CompositeDisposable _disposables = new();

    public ProfilesViewModel(
        ICommandBus commandBus,
        ICurrentProfile currentProfile,
        IEventBus eventBus,
        IProfileQueryService profileQuery,
        ISettingsQueryService settingsQuery,
        IViewModelFactory vmFactory,
        IUserInteraction ui,
        ProfileAutoGridBuilder autoGridBuilder)
    {
        Guard.NotNull(commandBus);
        Guard.NotNull(currentProfile);
        Guard.NotNull(eventBus);
        Guard.NotNull(settingsQuery);
        Guard.NotNull(ui);

        // Dependencies:
        _profileQuery = profileQuery.NotNull();
        _vmFactory = vmFactory.NotNull();
        AutoGridBuilder = autoGridBuilder.NotNull();

        // Actions:
        OpenAddProfileFlyoutCommand = new ActionCommand(_ => {
            IsAddEditProfileVisible = !IsAddEditProfileVisible;
            if (IsAddEditProfileVisible)
            {
                EditingProfile = vmFactory.CreateProfile(null);
                EditingProfile.CommitProfileCommand
                    .OnOneTimeExecutedBooleanAction()
                    .Subscribe(async _ => {
                        IsAddEditProfileVisible = false;
                        await ReloadListAsync();
                    })
                    .DisposeWith(_disposables);
            }
        });

        OpenEditProfileFlyoutCommand = new ActionCommand(_ => {
            EditingProfile = Profiles.FirstOrDefault(x => x.IsSelected);
            EditingProfile?.CommitProfileCommand
                .OnOneTimeExecutedBooleanAction()
                .Subscribe(_ => IsAddEditProfileVisible = false)
                .DisposeWith(_disposables);
            IsAddEditProfileVisible = EditingProfile is not null;
        });

        DeleteProfileCommand = new ActionCommand(async _ => {
            IsAddEditProfileVisible = false;
            var selectedProfile = Profiles.FirstOrDefault(x => x.IsSelected);
            if (selectedProfile is null)
            {
                ui.ShowWarning("No profile selected.");
                return;
            }
            if (!ui.AskForConfirmation($"Are you sure you want to delete the selected '{selectedProfile.Name}' profile?", "Delete profile"))
                return;

            Profiles.Remove(selectedProfile);

            if (currentProfile.Profile?.Id == selectedProfile.Id)
            {
                currentProfile.CloseProfile();
            }

            if (selectedProfile.Id is not null)
            {
                await commandBus.SendAsync(new ProfileDeleteCommand(selectedProfile.Id.Value));
            }
        });

        // Subscriptions:
        Deactivated.Executed
            .Subscribe(_ => IsAddEditProfileVisible = false)
            .DisposeWith(_disposables);

        eventBus.WhenFired<ProfileLoadingErrorEvent>()
            .Subscribe(args => ui.ShowWarning(args.Reason))
            .DisposeWith(_disposables);

        // Final preparation:
        Task.Run(() => ReloadListAsync())
            .ContinueWith(_ =>
            {
                var settings = settingsQuery.Get();
                if (settings.AutoLoadPreviouslyOpenedProfile && settings.AutoLoadProfile is not null)
                {
                    var profile = Profiles.FirstOrDefault(x => x.Id == settings.AutoLoadProfile);
                    profile?.LoadProfileCommand.Execute(null);
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    private async Task ReloadListAsync()
    {
        IsAddEditProfileVisible = false;
        var profileVms = (await _profileQuery.GetAllAsync())
            .Select(x => _vmFactory.CreateProfile(x))
            .ToList();
        Profiles.ReplaceItems(profileVms);
    }

    public IAutoGridBuilder AutoGridBuilder { get; }

    public ObservableCollection<IProfileViewModel> Profiles { get; }
        = new TypedObservableList<IProfileViewModel, ProfileViewModel>();

    public bool IsAddEditProfileVisible
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public IProfileViewModel? EditingProfile
    {
        get => GetOrDefault<IProfileViewModel?>();
        set => RaiseAndSetIfChanged(value);
    }

    [FilterContext]
    public string Filter
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public ICommand OpenAddProfileFlyoutCommand { get; }
    public ICommand OpenEditProfileFlyoutCommand { get; }
    public ICommand DeleteProfileCommand { get; }
}
