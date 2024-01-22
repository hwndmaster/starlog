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
using Genius.Starlog.UI.Controllers;

namespace Genius.Starlog.UI.Views;

public interface IProfilesViewModel : ITabViewModel, IDisposable
{
    DelayedObservableCollection<IProfileViewModel> Profiles { get; }
    bool IsAddEditProfileVisible { get; set; }
    IProfileViewModel? EditingProfile { get; }

    ICommand OpenAddProfileFlyoutCommand { get; }
    ICommand OpenEditProfileFlyoutCommand { get; }
    ICommand DeleteProfileCommand { get; }
}

// TODO: Cover with unit tests
public sealed class ProfilesViewModel : TabViewModelBase, IProfilesViewModel
{
    private readonly IComparisonController _comparisonController;
    private readonly IMainController _controller;
    private readonly IProfileQueryService _profileQuery;
    private readonly IViewModelFactory _vmFactory;
    private readonly CompositeDisposable _disposables = new();

    public ProfilesViewModel(
        ICommandBus commandBus,
        IComparisonController comparisonController,
        ICurrentProfile currentProfile,
        IEventBus eventBus,
        IMainController controller,
        IProfileQueryService profileQuery,
        IViewModelFactory vmFactory,
        IUserInteraction ui,
        ProfileAutoGridBuilder autoGridBuilder)
    {
        Guard.NotNull(commandBus);
        Guard.NotNull(currentProfile);
        Guard.NotNull(eventBus);
        Guard.NotNull(ui);

        // Dependencies:
        _comparisonController = comparisonController.NotNull();
        _controller = controller.NotNull();
        _profileQuery = profileQuery.NotNull();
        _vmFactory = vmFactory.NotNull();
        AutoGridBuilder = autoGridBuilder.NotNull();

        // Member initialization:
        DropAreas = new List<DropAreaViewModel>
        {
            new DropAreaViewModel("Create a new profile", (dropObj) =>
                {
                    if (dropObj is string[] fileDrop)
                    {
                        _controller.ShowAddProfileForPath(fileDrop[0]);
                    }
                }),
            new DropAreaViewModel("Open immediately", (dropObj) =>
                {
                    if (dropObj is string[] fileDrop)
                    {
                        _controller.ShowAnonymousProfileLoadSettingsViewAsync(fileDrop[0]);
                    }
                })
        };

        // Actions:
        CompareSelectedCommand = new ActionCommand(async _ => {
            var selectedProfiles = Profiles
                .Where(x => x.IsSelected && x.Profile is not null)
                .Take(2).ToArray();
            if (selectedProfiles.Length < 2)
            {
                ui.ShowWarning("Select two profiles to compare and try again.");
                return;
            }

            await _comparisonController.OpenProfilesForComparisonAsync(selectedProfiles[0].Profile!, selectedProfiles[1].Profile!);
        });

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
        Task.Run(() => ReloadListAsync());
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

        _controller.NotifyProfilesAreLoaded();
    }

    public IAutoGridBuilder AutoGridBuilder { get; }

    public DelayedObservableCollection<IProfileViewModel> Profiles { get; }
        = new TypedObservableCollection<IProfileViewModel, ProfileViewModel>();

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

    public ICollection<DropAreaViewModel> DropAreas
    {
        get => GetOrDefault<ICollection<DropAreaViewModel>>();
        set => RaiseAndSetIfChanged(value);
    }

    public bool ComparisonFeatureEnabled => App.ComparisonFeatureEnabled;

    public ICommand CompareSelectedCommand { get; }
    public ICommand DeleteProfileCommand { get; }
    public ICommand OpenAddProfileFlyoutCommand { get; }
    public ICommand OpenEditProfileFlyoutCommand { get; }
}
