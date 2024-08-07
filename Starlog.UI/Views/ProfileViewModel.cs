using System.Reactive.Linq;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.UI.Forms.Validation;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Controllers;
using Genius.Starlog.UI.Views.ProfileSettings;

namespace Genius.Starlog.UI.Views;

public interface IProfileViewModel : ISelectable
{
    void CopyFrom(IProfileViewModel source, string? nameSuffix = null);

    Guid? Id { get; }
    Profile? Profile { get; }
    string Name { get; set; }
    string Source { get; }
    IProfileSettingsViewModel ProfileSettings { get; }
    IActionCommand CommitProfileCommand { get; }
    IActionCommand LoadProfileCommand { get; }
    IActionCommand LocateCommand { get; }
}

public sealed class ProfileViewModel : DisposableViewModelBase, IProfileViewModel
{
    private readonly ICommandBus _commandBus;
    private readonly IProfileQueryService _profileQuery;
    private readonly IMainController _controller;
    private readonly IUserInteraction _ui;

    private Profile? _profile;

    public ProfileViewModel(
        Profile? profile,
        ICommandBus commandBus,
        IEventBus eventBus,
        IMainController controller,
        IProfileLoadingController profileLoadingController,
        IProfileQueryService profileQuery,
        IProfileSettingsViewModelFactory vmFactory,
        IUserInteraction ui)
    {
        Guard.NotNull(eventBus);
        Guard.NotNull(profileLoadingController);
        Guard.NotNull(vmFactory);

        // Dependencies:
        _commandBus = commandBus.NotNull();
        _controller = controller.NotNull();
        _profileQuery = profileQuery.NotNull();
        _ui = ui.NotNull();

        // Members initialization:
        _profile = profile;
        ProfileSettings = vmFactory.CreateProfileSettings(_profile?.Settings);

        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(Name)));

        InitializeProperties(() =>
        {
            ResetForm();
        });

        // Subscriptions:
        eventBus.WhenFired<ProfileLastOpenedUpdatedEvent>()
            .Where(eventArgs => _profile is not null && eventArgs.ProfileId == _profile.Id)
            .Subscribe(args => LastOpened = args.LastOpened)
            .DisposeWith(Disposer);

        // Actions:
        CommitProfileCommand = new ActionCommand(async _ => await CommitProfileAsync());
        LoadProfileCommand = new ActionCommand(async _ => await profileLoadingController.LoadProfileAsync(_profile!));
        LocateCommand = new ActionCommand(_ => _controller.Locate(_profile!),
            _ => _profile is not null);
        ResetCommand = new ActionCommand(_ => ResetForm(), _ => _profile is not null);
    }

    public void CopyFrom(IProfileViewModel source, string? nameSuffix = null)
    {
        if (source is not ProfileViewModel sourceProfile)
            return;

        Name = sourceProfile.Name + (nameSuffix ?? string.Empty);
        ProfileSettings.CopyFrom(sourceProfile.ProfileSettings);
    }

    private async Task<bool> CommitProfileAsync()
    {
        Validate();

        if (HasErrors)
        {
            _ui.ShowWarning(StringResources.ValidationError);
            return false;
        }

        var profileSettings = ProfileSettings.CommitChanges();
        if (profileSettings is null)
        {
            return false;
        }

        if (_profile is null)
        {
            var profileId = await _commandBus.SendAsync(new ProfileCreateCommand
            {
                Name = Name,
                Settings = profileSettings
            });
            _profile = await _profileQuery.FindByIdAsync(profileId);
        }
        else
        {
            await _commandBus.SendAsync(new ProfileUpdateCommand(_profile.Id)
            {
                Name = Name,
                Settings = profileSettings
            });
        }

        OnPropertyChanged(nameof(Source));

        return true;
    }

    private void ResetForm()
    {
        Name = _profile?.Name ?? Name;
        LastOpened = _profile?.LastOpened;

        ProfileSettings.ResetForm();
    }

    public Guid? Id => _profile?.Id;
    public Profile? Profile => _profile;

    public string PageTitle => _profile is null ? "Add profile" : "Edit profile";

    public string Name
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public string Source
    {
        get => ProfileSettings.Source;
    }

    public DateTime? LastOpened
    {
        get => GetOrDefault<DateTime?>();
        set => RaiseAndSetIfChanged(value);
    }

    public IProfileSettingsViewModel ProfileSettings { get; private set; }

    public bool IsSelected
    {
        get => GetOrDefault<bool>();
        set => RaiseAndSetIfChanged(value);
    }

    public IActionCommand CommitProfileCommand { get; }
    public IActionCommand LoadProfileCommand { get; }
    public IActionCommand LocateCommand { get; }
    public IActionCommand ResetCommand { get; }
}
