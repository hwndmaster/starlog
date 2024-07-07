using System.Collections.ObjectModel;
using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.ProfileFilters;

public interface IProfileFilterViewModel
{
    ProfileFilterBase? ProfileFilter { get; }
    IActionCommand CommitFilterCommand { get; }
}

public sealed class ProfileFilterViewModel : ViewModelBase, IProfileFilterViewModel
{
    private readonly ICommandBus _commandBus;
    private readonly ICurrentProfile _currentProfile;
    private readonly IUserInteraction _ui;
    private ProfileFilterBase? _profileFilter;

    public ProfileFilterViewModel(
        ProfileFilterBase? profileFilter,
        ICommandBus commandBus,
        ICurrentProfile currentProfile,
        ILogContainer logContainer,
        ILogFilterContainer logFilterContainer,
        IUserInteraction ui,
        IProfileFilterViewModelFactory vmFactory)
    {
        Guard.NotNull(logContainer);
        Guard.NotNull(logFilterContainer);
        Guard.NotNull(vmFactory);

        // Dependencies:
        _commandBus = commandBus.NotNull();
        _currentProfile = currentProfile.NotNull();
        _ui = ui.NotNull();

        // Members initialization:
        _profileFilter = profileFilter;
        FilterTypeCanBeChanged = _profileFilter is null;

        foreach (var logFilter in logFilterContainer.GetLogFilters())
        {
            /* TODO: To keep it for a while and check why I have added it before. With this condition
                     it fails when adding a custom field filter from smart contextmenu
            if (logFilter.Id == FieldProfileFilter.LogFilterId && logContainer.GetFields().GetThreadFieldIfAny() is null)
            {
                continue;
            }*/
            FilterTypes.Add(vmFactory.CreateProfileFilterSettings(logFilter, _profileFilter));
        }

        InitializeProperties(() =>
        {
            if (_profileFilter is not null)
            {
                Reconcile();
                FilterSettings.ResetChanges();
            }
        });

        // Actions:
        CommitFilterCommand = new ActionCommand(async _ => await CommitFilterAsync());
        ResetCommand = new ActionCommand(_ => FilterSettings.ResetChanges(), _ => _profileFilter is not null);
    }

    public void Reconcile()
    {
        if (_profileFilter is null)
        {
            return;
        }

        FilterSettings = FilterTypes.First(x => x.ProfileFilter.LogFilter.Id == _profileFilter.LogFilter.Id);
    }

    private async Task<bool> CommitFilterAsync()
    {
        Guard.NotNull(_currentProfile.Profile);

        if (HasErrors || FilterSettings.HasErrors)
        {
            _ui.ShowWarning(StringResources.ValidationError);
            return false;
        }

        FilterSettings.CommitChanges();

        var commandResult = await _commandBus.SendAsync(new ProfileFilterCreateOrUpdateCommand
        {
            ProfileId = _currentProfile.Profile.Id,
            ProfileFilter = FilterSettings.ProfileFilter
        });

        if (_profileFilter is null)
        {
            _profileFilter = _currentProfile.Profile.Filters.First(x => x.Id == commandResult.ProfileFiltersAdded[0]);
        }

        FilterTypeCanBeChanged = false;

        return true;
    }

    public ProfileFilterBase? ProfileFilter => _profileFilter;

    public ObservableCollection<IProfileFilterSettingsViewModel> FilterTypes { get; } = new();

    public string PageTitle => _profileFilter is null ? "Add filter" : "Edit filter";

    public IProfileFilterSettingsViewModel FilterSettings
    {
        get => GetOrDefault(FilterTypes[0]);
        set => RaiseAndSetIfChanged(value);
    }

    public bool FilterTypeCanBeChanged
    {
        get => GetOrDefault(true);
        set => RaiseAndSetIfChanged(value);
    }

    public IActionCommand CommitFilterCommand { get; }
    public IActionCommand ResetCommand { get; }
}
