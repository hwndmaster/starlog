using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.UI.Forms.Validation;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Controllers;

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
    private readonly IMainController _controller;
    private readonly IViewModelFactory _vmFactory;
    private readonly IUserInteraction _ui;

    private Profile? _profile;

    public ProfileViewModel(
        Profile? profile,
        ICommandBus commandBus,
        IMainController controller,
        IProfileQueryService profileQuery,
        IViewModelFactory vmFactory,
        IUserInteraction ui)
    {
        // Dependencies:
        _commandBus = commandBus.NotNull();
        _controller = controller.NotNull();
        _profileQuery = profileQuery.NotNull();
        _vmFactory = vmFactory.NotNull();
        _ui = ui.NotNull();

        // Members initialization:
        _profile = profile;
        ProfileSettings = _vmFactory.CreateProfileSettings(_profile?.Settings);

        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(Name)));
        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(Path)));
        AddValidationRule(new PathExistsValidationRule(nameof(Path)));

        InitializeProperties(() =>
        {
            ResetForm();
        });

        // Actions:
        CommitProfileCommand = new ActionCommand(_ => CommitProfile());
        LoadProfileCommand = new ActionCommand(async _ => await _controller.LoadProfileAsync(_profile!));
        ResetCommand = new ActionCommand(_ => ResetForm(), _ => _profile is not null);
    }

    public void CopyFrom(IProfileViewModel source, string? nameSuffix = null)
    {
        if (source is not ProfileViewModel sourceProfile)
            return;

        Name = sourceProfile.Name + (nameSuffix ?? string.Empty);
        Path = sourceProfile.Path;
        ProfileSettings.CopyFrom(sourceProfile.ProfileSettings);
    }

    private async Task<bool> CommitProfile()
    {
        Validate();

        if (HasErrors)
        {
            _ui.ShowWarning("Cannot proceed while there are errors in the form.");
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
                Path = Path,
                Settings = profileSettings
            });
            _profile = await _profileQuery.FindByIdAsync(profileId);
            ProfileSettings.ResetForm(_profile!.Settings);
        }
        else
        {
            await _commandBus.SendAsync(new ProfileUpdateCommand(_profile.Id)
            {
                Name = Name,
                Path = Path,
                Settings = profileSettings
            });
        }

        return true;
    }

    private void ResetForm()
    {
        Name = _profile?.Name ?? Name;
        Path = _profile?.Path ?? Path;

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

    public string Path
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public IProfileSettingsViewModel ProfileSettings
    {
        get => GetOrDefault(default(IProfileSettingsViewModel));
        set => RaiseAndSetIfChanged(value);
    }

    public bool IsSelected
    {
        get => GetOrDefault<bool>();
        set => RaiseAndSetIfChanged(value);
    }

    public IActionCommand CommitProfileCommand { get; }

    public IActionCommand LoadProfileCommand { get; }

    public IActionCommand ResetCommand { get; }
}
