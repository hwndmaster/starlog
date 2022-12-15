using System.Collections.ObjectModel;
using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.ViewModels;

public interface IProfileFilterViewModel
{
    ProfileFilterBase? ProfileFilter { get; }
    IActionCommand CommitFilterCommand { get; }
}

public sealed class ProfileFilterViewModel : ViewModelBase, IProfileFilterViewModel
{
    private readonly ICommandBus _commandBus;
    private readonly ICurrentProfile _currentProfile;
    private readonly ILogFilterContainer _logFilterContainer;
    private readonly IUserInteraction _ui;
    private ProfileFilterBase? _profileFilter;

    public ProfileFilterViewModel(
        ProfileFilterBase? profileFilter,
        ICommandBus commandBus,
        ICurrentProfile currentProfile,
        ILogFilterContainer logFilterContainer,
        IUserInteraction ui,
        IViewModelFactory vmFactory)
    {
        // Dependencies:
        _commandBus = commandBus.NotNull();
        _currentProfile = currentProfile.NotNull();
        _logFilterContainer = logFilterContainer.NotNull();
        _ui = ui.NotNull();

        // Members initialization:
        _profileFilter = profileFilter;

        foreach (var logFilter in _logFilterContainer.GetLogFilters())
        {
            FilterTypes.Add(vmFactory.CreateProfileFilterSettings(logFilter, _profileFilter));
        }

        InitializeProperties(() =>
        {
            if (_profileFilter is not null)
            {
                ResetForm();
                Reconcile();
            }
        });

        // Actions:
        CommitFilterCommand = new ActionCommand(_ => CommitFilter());
        ResetCommand = new ActionCommand(_ => ResetForm(), _ => _profileFilter is not null);
    }

    public void Reconcile()
    {
        if (_profileFilter is null)
        {
            return;
        }

        FilterSettings = FilterTypes.First(x => x.ProfileFilter.LogFilter.Id == _profileFilter.LogFilter.Id);
    }

    private async Task<bool> CommitFilter()
    {
        Guard.NotNull(_currentProfile.Profile);

        if (HasErrors || FilterSettings.HasErrors)
        {
            _ui.ShowWarning("Cannot proceed while there are errors in the form.");
            return false;
        }

        var commandResult = await _commandBus.SendAsync(new ProfileFilterCreateOrUpdateCommand
        {
            ProfileId = _currentProfile.Profile.Id,
            ProfileFilter = FilterSettings.ProfileFilter
        });

        if (_profileFilter is null)
        {
            _profileFilter = _currentProfile.Profile.Filters.First(x => x.Id == commandResult.ProfileFiltersAdded[0]);
        }

        return true;
    }

    private void ResetForm()
    {
        FilterSettings = _profileFilter is null
            ? FilterSettings
            : FilterTypes.First(x => x.ProfileFilter.LogFilter.Id == _profileFilter.LogFilter.Id);
    }

    public ProfileFilterBase? ProfileFilter => _profileFilter;

    public ObservableCollection<IProfileFilterSettingsViewModel> FilterTypes { get; } = new();

    public string PageTitle => _profileFilter is null ? "Add filter" : "Edit filter";

    public IProfileFilterSettingsViewModel FilterSettings
    {
        get => GetOrDefault(FilterTypes[0]);
        set => RaiseAndSetIfChanged(value);
    }

    public IActionCommand CommitFilterCommand { get; }
    public IActionCommand ResetCommand { get; }
}
