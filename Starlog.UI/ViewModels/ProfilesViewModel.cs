using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.UI.Forms;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.AutoGridBuilders;
using Genius.Starlog.UI.Helpers;

namespace Genius.Starlog.UI.ViewModels;

public interface IProfilesViewModel : ITabViewModel
{
    bool IsAddEditProfileVisible { get; set; }
    IProfileViewModel? EditingProfile { get; }
    ICommand OpenAddProfileFlyoutCommand { get; }
}

internal sealed class ProfilesViewModel : TabViewModelBase, IProfilesViewModel, IDisposable
{
    private readonly IProfileQueryService _profileQuery;
    private readonly IViewModelFactory _vmFactory;
    private readonly CompositeDisposable _disposables = new();

    public ProfilesViewModel(
        ICommandBus commandBus,
        IProfileQueryService profileQuery,
        IViewModelFactory vmFactory,
        IUserInteraction ui,
        ProfileAutoGridBuilder autoGridBuilder)
    {
        Guard.NotNull(commandBus);
        Guard.NotNull(ui);

        _profileQuery = profileQuery.NotNull();
        _vmFactory = vmFactory.NotNull();
        AutoGridBuilder = autoGridBuilder.NotNull();

        OpenAddProfileFlyoutCommand = new ActionCommand(_ => {
            IsAddEditProfileVisible = !IsAddEditProfileVisible;
            if (IsAddEditProfileVisible)
            {
                EditingProfile = vmFactory.CreateProfile(null);
                EditingProfile.CommitProfileCommand.Executed
                    .Where(x => x)
                    .Take(1)
                    .Subscribe(async _ => {
                        IsAddEditProfileVisible = false;
                        await ReloadListAsync();
                    })
                    .DisposeWith(_disposables);
            }
        });

        OpenEditProfileFlyoutCommand = new ActionCommand(_ => {
            EditingProfile = Profiles.FirstOrDefault(x => x.IsSelected);
            EditingProfile?.CommitProfileCommand.Executed
                .Where(x => x)
                .Take(1)
                .Subscribe(_ =>
                    IsAddEditProfileVisible = false)
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

            if (selectedProfile.Id is not null)
            {
                await commandBus.SendAsync(new ProfileDeleteCommand(selectedProfile.Id.Value));
            }
        });

        Deactivated.Executed
            .Subscribe(_ => IsAddEditProfileVisible = false)
            .DisposeWith(_disposables);

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
