using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Views.ProfileFilters;

namespace Genius.Starlog.UI.Views.LogSearchAndFiltering;

public interface ILogsFilteringViewModel : IDisposable
{
    LogRecordFilterContext CreateContext();
    void DropAllFilters();
    void DropBookmarkedFilter();
    void ShowFlyoutForAddingNewFilter(ProfileFilterBase? profileFilter);

    IObservable<Unit> FilterChanged { get; }
}

// TODO: Cover with unit tests
public sealed class LogsFilteringViewModel : ViewModelBase, ILogsFilteringViewModel
{
    private readonly ICommandBus _commandBus;
    private readonly ICurrentProfile _currentProfile;
    private readonly ILogContainer _logContainer;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly IUserInteraction _ui;
    private readonly IViewModelFactory _vmFactory;
    private readonly LogFilterCategoryViewModel<LogFileViewModel> _filesCategory = new("Files", "FolderFiles32", sort: true);
    private readonly LogFilterCategoryViewModel<LogFilterViewModel> _quickFiltersCategory = new("Quick filters", "FolderDown32", expanded: true);
    private readonly LogFilterCategoryViewModel<LogFilterViewModel> _userFiltersCategory = new("User filters", "FolderFavs32", expanded: true, canAddChildren: true);
    private readonly LogFilterCategoryViewModel<LogFilterViewModel> _bookmarkedCategory = new LogFilterBookmarkedCategoryViewModel();
    private readonly CompositeDisposable _subscriptions;
    private readonly ISubject<Unit> _filterChanged = new Subject<Unit>();
    private bool _suspendUpdate = false;


    public LogsFilteringViewModel(
        ICommandBus commandBus,
        ICurrentProfile currentProfile,
        ILogContainer logContainer,
        IQuickFilterProvider quickFilterProvider,
        IUiDispatcher uiDispatcher,
        IUserInteraction ui,
        IViewModelFactory vmFactory)
    {
        Guard.NotNull(quickFilterProvider);

        // Dependencies:
        _commandBus = commandBus.NotNull();
        _currentProfile = currentProfile.NotNull();
        _logContainer = logContainer.NotNull();
        _uiDispatcher = uiDispatcher.NotNull();
        _ui = ui.NotNull();
        _vmFactory = vmFactory.NotNull();

        // Members initialization:
        _quickFiltersCategory.AddItems(quickFilterProvider.GetQuickFilters()
            .Select(x => new LogFilterViewModel(x, isUserDefined: false)));

        SubscribeToPinningEvents(_quickFiltersCategory.CategoryItems, () => _filterChanged.OnNext(Unit.Default));

        FilterCategories.Add(_filesCategory);
        FilterCategories.Add(_quickFiltersCategory);
        FilterCategories.Add(_userFiltersCategory);
        FilterCategories.Add(_bookmarkedCategory);

        // Subscriptions:
        _subscriptions = new(
            _currentProfile.ProfileClosed
                .Subscribe(_ =>
                {
                    _suspendUpdate = true;

                    _uiDispatcher.BeginInvoke(() =>
                    {
                        IsAddEditProfileFilterVisible = false;
                        _filesCategory.CategoryItems.Clear();
                        _filesCategory.CategoryItemsView.View.Refresh();
                        _userFiltersCategory.CategoryItems.Clear();
                    });
                }),
            _currentProfile.ProfileChanged
                .Subscribe(profile =>
                {
                    if (profile is null)
                    {
                        return;
                    }

                    _uiDispatcher.BeginInvoke(() =>
                    {
                        AddFiles(_logContainer.GetFiles());
                        AddUserFilters(profile.Filters);
                        _suspendUpdate = false;
                    });
                }),
            _logContainer.FileAdded
                .Where(_ => !_suspendUpdate)
                .Subscribe(x => _uiDispatcher.BeginInvoke(() =>
                    AddFiles(new[] { x }))),
            _logContainer.FileRenamed
                .Where(_ => !_suspendUpdate)
                .Subscribe(x =>
                {
                    var item = _filesCategory.CategoryItems.FirstOrDefault(ci => ci.File == x.OldRecord);
                    _uiDispatcher.BeginInvoke(() => item?.HandleFileRenamed(x.NewRecord));
                }),
            _logContainer.FileRemoved
                .Where(_ => !_suspendUpdate)
                .Subscribe(x =>
                {
                    var item = _filesCategory.CategoryItems.FirstOrDefault(ci => ci.File == x);
                    if (item is not null)
                        _uiDispatcher.BeginInvoke(() => _filesCategory.RemoveItem(item));
                }),

            _userFiltersCategory.AddChildCommand.Executed
                .Subscribe(_ => ShowFlyoutForAddingNewFilter(null)),

            SelectedFilters.WhenCollectionChanged()
                .Throttle(TimeSpan.FromMilliseconds(50))
                .Subscribe(_ =>
                {
                    IsAddEditProfileFilterVisible = false;
                    _filterChanged.OnNext(Unit.Default);
                }),

            this.WhenChanged(x => x.IsOr)
                .Subscribe(_ => _filterChanged.OnNext(Unit.Default))
        );
    }

    public LogRecordFilterContext CreateContext()
    {
        if (SelectedFilters.Any(x => x == _bookmarkedCategory))
        {
            return new(HasAnythingSpecified: true,
                new HashSet<string>(0), ImmutableArray<ProfileFilterBase>.Empty, ShowBookmarked: true, UseOrCombination: IsOr);
        }

        var filters = SelectedFilters
            .Union(_filesCategory.CategoryItems.Where(x => x.IsPinned))
            .Union(_quickFiltersCategory.CategoryItems.Where(x => x.IsPinned))
            .Union(_userFiltersCategory.CategoryItems.Where(x => x.IsPinned))
            .ToList();

        var filesSelected = filters.OfType<LogFileViewModel>()
            .Select(x => x.File.FileName)
            .ToHashSet();
        var filtersSelected = filters.OfType<LogFilterViewModel>()
            .Select(x => x.Filter)
            .ToImmutableArray();

        return new(HasAnythingSpecified: filesSelected.Any() || filtersSelected.Any(),
            filesSelected, filtersSelected, ShowBookmarked: false, UseOrCombination: IsOr);
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
    }

    public void DropAllFilters()
    {
        SelectedFilters.Clear();
    }

    public void DropBookmarkedFilter()
    {
        SelectedFilters.Remove(_bookmarkedCategory);
    }

    public void ShowFlyoutForAddingNewFilter(ProfileFilterBase? profileFilter)
    {
        IsAddEditProfileFilterVisible = !IsAddEditProfileFilterVisible;
        if (IsAddEditProfileFilterVisible)
        {
            EditingProfileFilter = _vmFactory.CreateProfileFilter(profileFilter);
            EditingProfileFilter.CommitFilterCommand
                .OnOneTimeExecutedBooleanAction()
                .Subscribe(commandResult => {
                    if (!commandResult || EditingProfileFilter.ProfileFilter is null)
                        return;
                    var vm = AddUserFilters(new [] { EditingProfileFilter.ProfileFilter }).First();
                    IsAddEditProfileFilterVisible = false;
                    SelectedFilters.Clear();
                    SelectedFilters.Add(vm);
                })
                .DisposeWith(_subscriptions!);
        }
    }

    private static void SubscribeToPinningEvents(IEnumerable<LogFilterViewModel> items, Action handler)
    {
        foreach (var item in items)
        {
            if (item.CanPin)
            {
                item.WhenChanged(x => x.IsPinned).Subscribe(_ => handler());
            }
        }
    }

    private IEnumerable<LogFilterViewModel> AddUserFilters(IEnumerable<ProfileFilterBase> userFilters)
    {
        var vms = userFilters.Select(x =>
        {
            var vm = new LogFilterViewModel(x, isUserDefined: true);
            vm.ModifyCommand.Executed.Subscribe(_ =>
            {
                EditingProfileFilter = _vmFactory.CreateProfileFilter(vm.Filter);
                EditingProfileFilter?.CommitFilterCommand
                    .OnOneTimeExecutedBooleanAction()
                    .Subscribe(_ =>
                    {
                        IsAddEditProfileFilterVisible = false;
                        vm.Reconcile();
                        _filterChanged.OnNext(Unit.Default);
                    })
                    .DisposeWith(_subscriptions);
                IsAddEditProfileFilterVisible = EditingProfileFilter is not null;
            });
            vm.DeleteCommand.Executed.Subscribe(async _ =>
            {
                if (_ui.AskForConfirmation($"You're about to delete a filter named '{vm.Filter.Name}'. Proceed?", "Deletion confirmation"))
                {
                    await _commandBus.SendAsync(new ProfileFilterDeleteCommand(_currentProfile.Profile.NotNull().Id, vm.Filter.Id));
                    _userFiltersCategory.RemoveItem(vm);
                }
            });
            vm.WhenChanged(x => x.IsSelected).Subscribe(_ => IsAddEditProfileFilterVisible = false);
            return vm;
        }).ToList();
        _userFiltersCategory.AddItems(vms);
        SubscribeToPinningEvents(vms, () => _filterChanged.OnNext(Unit.Default));
        return vms;
    }

    private void AddFiles(IEnumerable<FileRecord> files)
    {
        _filesCategory.AddItems(
            files.OrderBy(x => x.FileName)
                .Select(x => new LogFileViewModel(x))
        );
    }

    public IObservable<Unit> FilterChanged => _filterChanged;

    public ObservableCollection<ILogFilterNodeViewModel> FilterCategories { get; } = new();
    public ObservableCollection<ILogFilterNodeViewModel> SelectedFilters { get; } = new();

    public bool IsAddEditProfileFilterVisible
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public IProfileFilterViewModel? EditingProfileFilter
    {
        get => GetOrDefault<IProfileFilterViewModel?>();
        set => RaiseAndSetIfChanged(value);
    }

    public bool IsOr
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }
}
