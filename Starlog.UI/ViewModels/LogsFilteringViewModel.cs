using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Helpers;

namespace Genius.Starlog.UI.ViewModels;

public interface ILogsFilteringViewModel
{
    LogFilterContext CreateContext();

    IObservable<Unit> FilterChanged { get; }
}

public sealed class LogsFilteringViewModel : ViewModelBase, ILogsFilteringViewModel
{
    private readonly ICommandBus _commandBus;
    private readonly ICurrentProfile _currentProfile;
    private readonly ILogContainer _logContainer;
    private readonly ILogFiltersHelper _helper;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly IUserInteraction _ui;
    private readonly IViewModelFactory _vmFactory;
    private readonly LogFilterCategoryViewModel<LogFileViewModel> _filesCategory = new("Files", "FolderFiles32", sort: true);
    private readonly LogFilterCategoryViewModel<LogFilterViewModel> _quickFiltersCategory = new("Quick filters", "FolderDown32", expanded: true);
    private readonly LogFilterCategoryViewModel<LogFilterViewModel> _userFiltersCategory = new("User filters", "FolderFavs32", expanded: true, canAddChildren: true);
    private readonly CompositeDisposable _subscriptions = new(); // TODO: Dispose this
    private readonly ISubject<Unit> _filterChanged = new Subject<Unit>();
    private bool _suspendUpdate = false;


    public LogsFilteringViewModel(
        ICommandBus commandBus,
        ICurrentProfile currentProfile,
        ILogContainer logContainer,
        ILogFiltersHelper helper,
        IUiDispatcher uiDispatcher,
        IUserInteraction ui,
        IViewModelFactory vmFactory)
    {
        _commandBus = commandBus.NotNull();
        _currentProfile = currentProfile.NotNull();
        _logContainer = logContainer.NotNull();
        _helper = helper.NotNull();
        _uiDispatcher = uiDispatcher.NotNull();
        _ui = ui.NotNull();
        _vmFactory = vmFactory.NotNull();

        _helper.InitializeQuickFiltersCategory(_quickFiltersCategory);
        _helper.InitializePinSubscription(_quickFiltersCategory.CategoryItems, () => _filterChanged.OnNext(Unit.Default));

        FilterCategories.Add(_filesCategory);
        FilterCategories.Add(_quickFiltersCategory);
        FilterCategories.Add(_userFiltersCategory);

        _subscriptions.Add(_currentProfile.ProfileChanging
            .Subscribe(_ =>
            {
                _suspendUpdate = true;

                _uiDispatcher.BeginInvoke(() =>
                {
                    _filesCategory.CategoryItems.Clear();
                    _filesCategory.CategoryItemsView.View.Refresh();
                    _userFiltersCategory.CategoryItems.Clear();
                });
            }));
        _subscriptions.Add(_currentProfile.ProfileChanged
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
            }));
        _subscriptions.Add(_logContainer.FileAdded
            .Where(_ => !_suspendUpdate)
            .Subscribe(x => AddFiles(new [] { x })));

        _userFiltersCategory.AddChildCommand.Executed.Subscribe(_ =>
        {
            IsAddEditProfileFilterVisible = !IsAddEditProfileFilterVisible;
            if (IsAddEditProfileFilterVisible)
            {
                EditingProfileFilter = vmFactory.CreateProfileFilter(null);
                EditingProfileFilter.CommitFilterCommand.Executed
                    .Where(x => x)
                    .Take(1)
                    .Subscribe(commandResult => {
                        if (!commandResult || EditingProfileFilter.ProfileFilter is null)
                            return;
                        var vm = AddUserFilters(new [] { EditingProfileFilter.ProfileFilter }).First();
                        IsAddEditProfileFilterVisible = false;
                        SelectedFilters.Clear();
                        SelectedFilters.Add(vm);
                    })
                    .DisposeWith(_subscriptions);
            }
        });

        SelectedFilters.WhenCollectionChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(_ => _filterChanged.OnNext(Unit.Default));
    }

    public LogFilterContext CreateContext()
    {
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

        return new(filesSelected, filtersSelected);
    }

    private IEnumerable<LogFilterViewModel> AddUserFilters(IEnumerable<ProfileFilterBase> userFilters)
    {
        var vms = userFilters.Select(x =>
        {
            var vm = new LogFilterViewModel(x, isUserDefined: true);
            vm.ModifyCommand.Executed.Subscribe(_ =>
            {
                EditingProfileFilter = _vmFactory.CreateProfileFilter(vm.Filter);
                EditingProfileFilter?.CommitFilterCommand.Executed
                    .Where(x => x)
                    .Take(1)
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
        _helper.InitializePinSubscription(vms, () => _filterChanged.OnNext(Unit.Default));
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
}
