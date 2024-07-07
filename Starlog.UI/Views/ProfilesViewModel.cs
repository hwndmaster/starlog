using System.Collections.Immutable;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Genius.Atom.Infrastructure.Tasks;
using Genius.Atom.UI.Forms.Controls.AutoGrid;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Starlog.Core;
using Genius.Starlog.Core.Clients;
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

public sealed class ProfilesViewModel : TabViewModelBase, IProfilesViewModel
{
    private readonly IComparisonController _comparisonController;
    private readonly IMainController _controller;
    private readonly IProfileQueryService _profileQuery;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly CompositeDisposable _disposables = new();

    public ProfilesViewModel(
        IComparisonController comparisonController,
        ICurrentProfile currentProfile,
        IMainController controller,
        IProfileClient profileClient,
        IProfileLoadingController profileLoadingController,
        IProfileQueryService profileQuery,
        ISettingsClient settingsClient,
        IUiDispatcher uiDispatcher,
        IViewModelFactory viewModelFactory,
        IUserInteraction ui,
        ProfileAutoGridBuilder autoGridBuilder)
    {
        Guard.NotNull(currentProfile);
        Guard.NotNull(profileClient);
        Guard.NotNull(profileLoadingController);
        Guard.NotNull(settingsClient);
        Guard.NotNull(ui);
        Guard.NotNull(uiDispatcher);

        // Dependencies:
        _comparisonController = comparisonController.NotNull();
        _controller = controller.NotNull();
        _profileQuery = profileQuery.NotNull();
        _viewModelFactory = viewModelFactory.NotNull();
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
                        profileLoadingController.ShowAnonymousProfileLoadSettingsViewAsync(fileDrop[0]).RunAndForget();
                    }
                })
        };

        SortedColumns = settingsClient.GetProfilesViewSettings().GetSortedColumns()
            .Select(x => new ColumnSortingInfo(x.ColumnName, x.SortAsc))
            .ToImmutableList();

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
                EditingProfile = viewModelFactory.CreateProfile(null);
                EditingProfile.CommitProfileCommand
                    .OnOneTimeExecutedBooleanAction()
                    .SubscribeOnUiThread(async _ => {
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
                await profileClient.DeleteAsync(selectedProfile.Id.Value);
            }
        });

        // Subscriptions:
        Deactivated.Executed
            .Subscribe(_ => IsAddEditProfileVisible = false)
            .DisposeWith(_disposables);
        this.WhenChanged(x => x.SortedColumns)
            .Subscribe(async _ =>
            {
                var viewSettings = settingsClient.GetProfilesViewSettings();
                viewSettings.SetSortedColumns(SortedColumns.Select(x => (x.ColumnName, x.SortAsc)));
                await settingsClient.UpdateProfilesViewSettingsAsync(viewSettings);
            });

        // Final preparation:
        uiDispatcher.InvokeAsync(ReloadListAsync).RunAndForget();
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    private async Task ReloadListAsync()
    {
        IsAddEditProfileVisible = false;
        var profileVms = (await _profileQuery.GetAllAsync())
            .Select(x => _viewModelFactory.CreateProfile(x))
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

    public ImmutableList<ColumnSortingInfo> SortedColumns
    {
        get => GetOrDefault(ImmutableList<ColumnSortingInfo>.Empty);
        set => RaiseAndSetIfChanged(value);
    }

    public bool ComparisonFeatureEnabled => App.ComparisonFeatureEnabled;

    public ICommand CompareSelectedCommand { get; }
    public ICommand DeleteProfileCommand { get; }
    public ICommand OpenAddProfileFlyoutCommand { get; }
    public ICommand OpenEditProfileFlyoutCommand { get; }
}
