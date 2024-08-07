using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
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

    /// <summary>
    ///   Suspends or resumes the view model reaction over the profile being closed/opened.
    /// </summary>
    /// <param name="suspend"><c>true</c> to suspend event reaction, <c>false</c> to resume.</param>
    void SuspendResumeProfileReload(bool suspend);

    IObservable<Unit> FilterChanged { get; }
}

public sealed class LogsFilteringViewModel : DisposableViewModelBase, ILogsFilteringViewModel
{
    private readonly ICommandBus _commandBus;
    private readonly ICurrentProfile _currentProfile;
    private readonly IEventBus _eventBus;
    private readonly ILogContainer _logContainer;
    private readonly IProfileFilterViewModelFactory _logFilterViewModelFactory;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly IUserInteraction _ui;
    private readonly IViewModelFactory _vmFactory;
    private readonly LogFilterCategoryViewModel<LogSourceViewModel> _sourcesCategory = new("Sources", "FolderFiles32", sort: true);
    private readonly LogFilterCategoryViewModel<LogFilterViewModel> _quickFiltersCategory = new("Quick filters", "FolderDown32", expanded: true);
    private readonly LogFilterCategoryViewModel<LogFilterViewModel> _userFiltersCategory = new("User filters", "FolderFavs32", expanded: true, canAddChildren: true);
    private readonly LogFilterCategoryViewModel<LogFilterViewModel> _bookmarkedCategory = new LogFilterBookmarkedCategoryViewModel();
    private readonly LogFilterCategoryViewModel<LogFilterMessageParsingViewModel> _messageParsingCategory = new("Message parsing", "MessageParsing32", expanded: true, canAddChildren: true);
    private readonly Subject<Unit> _filterChanged = new();
    private bool _suspendUpdate;
    private bool _suspendProfileReloadHandling;


    public LogsFilteringViewModel(
        ICommandBus commandBus,
        ICurrentProfile currentProfile,
        IEventBus eventBus,
        ILogContainer logContainer,
        IProfileFilterViewModelFactory logFilterViewModelFactory,
        IQuickFilterProvider quickFilterProvider,
        IUiDispatcher uiDispatcher,
        IUserInteraction ui,
        IViewModelFactory vmFactory)
    {
        Guard.NotNull(quickFilterProvider);

        // Dependencies:
        _commandBus = commandBus.NotNull();
        _currentProfile = currentProfile.NotNull();
        _eventBus = eventBus.NotNull();
        _logContainer = logContainer.NotNull();
        _logFilterViewModelFactory = logFilterViewModelFactory.NotNull();
        _uiDispatcher = uiDispatcher.NotNull();
        _ui = ui.NotNull();
        _vmFactory = vmFactory.NotNull();

        // Members initialization:
        FilterCategories.Add(_sourcesCategory);
        FilterCategories.Add(_quickFiltersCategory);
        FilterCategories.Add(_userFiltersCategory);
        FilterCategories.Add(_bookmarkedCategory);
        FilterCategories.Add(_messageParsingCategory);
        _filterChanged.DisposeWith(Disposer);

        // Subscriptions:
        _currentProfile.ProfileClosed
            .Subscribe(_ =>
            {
                if (_suspendProfileReloadHandling)
                    return;
                _suspendUpdate = true;

                _uiDispatcher.Invoke(() =>
                {
                    IsAddEditProfileFilterVisible = false;
                    _sourcesCategory.CategoryItems.Clear();
                    _sourcesCategory.CategoryItemsView.View.Refresh();
                    foreach (var item in _userFiltersCategory.CategoryItems)
                        item.Dispose();
                    _userFiltersCategory.CategoryItems.Clear();
                    foreach (var item in _messageParsingCategory.CategoryItems)
                        item.Dispose();
                    _messageParsingCategory.CategoryItems.Clear();
                });
            }).DisposeWith(Disposer);
        _currentProfile.ProfileChanged
            .Subscribe(profile =>
            {
                if (_suspendProfileReloadHandling)
                    return;
                if (profile is null)
                    return;

                _uiDispatcher.Invoke(() =>
                {
                    _quickFiltersCategory.RemoveAll();
                    _quickFiltersCategory.AddItems(quickFilterProvider.GetQuickFilters()
                        .Select(x => new LogFilterViewModel(x, isUserDefined: false, _eventBus)));
                    SubscribeToPinningEvents(_quickFiltersCategory.CategoryItems, () => _filterChanged.OnNext(Unit.Default));
                    AddSources(_logContainer.GetSources());
                    AddUserFilters(profile.Filters);
                    AddMessageParsings(profile.MessageParsings);
                    _suspendUpdate = false;
                });
            }).DisposeWith(Disposer);
        _logContainer.SourceAdded
            .Where(_ => !_suspendUpdate)
            .Subscribe(x => _uiDispatcher.Invoke(() =>
                AddSources([x])))
            .DisposeWith(Disposer);
        _logContainer.SourceRenamed
            .Where(_ => !_suspendUpdate)
            .Subscribe(x =>
            {
                var item = _sourcesCategory.CategoryItems.FirstOrDefault(ci => ci.Source == x.OldRecord);
                _uiDispatcher.Invoke(() => item?.HandleSourceRenamed(x.NewRecord));
            }).DisposeWith(Disposer);
        _logContainer.SourceRemoved
            .Where(_ => !_suspendUpdate)
            .Subscribe(x =>
            {
                var item = _sourcesCategory.CategoryItems.FirstOrDefault(ci => ci.Source == x);
                if (item is not null)
                    _uiDispatcher.Invoke(() => _sourcesCategory.RemoveItem(item));
            }).DisposeWith(Disposer);

        _userFiltersCategory.AddChildCommand.Executed
            .Subscribe(_ => ShowFlyoutForAddingNewFilter(null))
            .DisposeWith(Disposer);

        // TODO: Cover with unit tests
        _messageParsingCategory.AddChildCommand.Executed
            .Subscribe(_ => ShowFlyoutForAddingNewMessageParsing())
            .DisposeWith(Disposer);

        SelectedFilters.WhenCollectionChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(_ =>
            {
                IsAddEditProfileFilterVisible = false;
                _filterChanged.OnNext(Unit.Default);
            }).DisposeWith(Disposer);

        this.WhenChanged(x => x.IsOr)
            .Subscribe(_ => _filterChanged.OnNext(Unit.Default))
            .DisposeWith(Disposer);
    }

    public LogRecordFilterContext CreateContext()
    {
        // TODO: Cover `messageParsings` with unit tests
        var messageParsingVms = SelectedFilters.OfType<LogFilterMessageParsingViewModel>().ToList();
        var messageParsings = messageParsingVms
            .Union(_messageParsingCategory.CategoryItems.Where(x => x.IsPinned))
            .Select(x => x.MessageParsing)
            .ToImmutableArray();

        if (SelectedFilters.Any(x => x == _bookmarkedCategory))
        {
            return new(HasAnythingSpecified: true,
                new HashSet<string>(0), ImmutableArray<ProfileFilterBase>.Empty, ShowBookmarked: true, UseOrCombination: IsOr,
                messageParsings);
        }

        var filters = SelectedFilters
            .Except(messageParsingVms)
            .Union(_sourcesCategory.CategoryItems.Where(x => x.IsPinned))
            .Union(_quickFiltersCategory.CategoryItems.Where(x => x.IsPinned))
            .Union(_userFiltersCategory.CategoryItems.Where(x => x.IsPinned))
            .ToList();

        var sourcesSelected = filters.OfType<LogSourceViewModel>()
            .Select(x => x.Source.DisplayName)
            .ToHashSet();
        var filtersSelected = filters.OfType<LogFilterViewModel>()
            .Select(x => x.Filter)
            .ToImmutableArray();

        return new(HasAnythingSpecified: sourcesSelected.Any() || filtersSelected.Any(),
            sourcesSelected, filtersSelected, ShowBookmarked: false, UseOrCombination: IsOr,
            messageParsings);
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
            EditingProfileFilter = _logFilterViewModelFactory.CreateProfileFilter(profileFilter);
            EditingProfileFilter.CommitFilterCommand
                .OnOneTimeExecutedBooleanAction()
                .Subscribe(commandResult => {
                    if (!commandResult || EditingProfileFilter.ProfileFilter is null)
                        return;
                    var vm = AddUserFilters([EditingProfileFilter.ProfileFilter]).First();
                    IsAddEditProfileFilterVisible = false;
                    SelectedFilters.Clear();
                    SelectedFilters.Add(vm);
                })
                .DisposeWith(Disposer);
        }
    }

    public void SuspendResumeProfileReload(bool suspend)
    {
        _suspendProfileReloadHandling = suspend;
    }

    private void ShowFlyoutForAddingNewMessageParsing()
    {
        IsAddEditMessageParsingVisible = !IsAddEditMessageParsingVisible;
        if (IsAddEditMessageParsingVisible)
        {
            EditingMessageParsing = _vmFactory.CreateMessageParsing(null);
            EditingMessageParsing.CommitCommand
                .OnOneTimeExecutedBooleanAction()
                .Subscribe(commandResult => {
                    if (!commandResult || EditingMessageParsing.MessageParsing is null)
                        return;
                    var vm = AddMessageParsings(new [] { EditingMessageParsing.MessageParsing }).First();
                    IsAddEditMessageParsingVisible = false;
                    SelectedFilters.Clear();
                    SelectedFilters.Add(vm);
                })
                .DisposeWith(Disposer);
        }
    }

    private static void SubscribeToPinningEvents<T>(IEnumerable<T> items, Action handler)
        where T : IViewModel, IHasPinnedFlag, IHasCanPinFlag
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
        var vms = userFilters.Select(profileFilter =>
        {
            var vm = new LogFilterViewModel(profileFilter, isUserDefined: true, _eventBus);
            vm.ModifyCommand.Executed.Subscribe(_ =>
            {
                EditingProfileFilter = _logFilterViewModelFactory.CreateProfileFilter(vm.Filter);
                EditingProfileFilter?.CommitFilterCommand
                    .OnOneTimeExecutedBooleanAction()
                    .Subscribe(_ =>
                    {
                        IsAddEditProfileFilterVisible = false;
                        vm.Reconcile();
                        _filterChanged.OnNext(Unit.Default);
                    })
                    .DisposeWith(Disposer);
                IsAddEditProfileFilterVisible = EditingProfileFilter is not null;
            }).DisposeWith(vm.DisposerForExternalSubscriptions);
            vm.DeleteCommand.Executed.SubscribeOnUiThread(async _ =>
            {
                if (_ui.AskForConfirmation($"You're about to delete a filter named '{vm.Filter.Name}'. Proceed?", "Deletion confirmation"))
                {
                    await _commandBus.SendAsync(new ProfileFilterDeleteCommand(_currentProfile.Profile.NotNull().Id, vm.Filter.Id));
                    vm.Dispose();
                    _userFiltersCategory.RemoveItem(vm);
                }
            }).DisposeWith(vm.DisposerForExternalSubscriptions);
            vm.WhenChanged(x => x.IsSelected)
                .Subscribe(_ => IsAddEditProfileFilterVisible = false)
                .DisposeWith(vm.DisposerForExternalSubscriptions);
            return vm;
        }).ToList();
        _userFiltersCategory.AddItems(vms);
        SubscribeToPinningEvents(vms, () => _filterChanged.OnNext(Unit.Default));
        return vms;
    }

    private IEnumerable<ILogFilterNodeViewModel> AddMessageParsings(IEnumerable<MessageParsing> messageParsings)
    {
        var vms = messageParsings.Select(x =>
        {
            var vm = _vmFactory.CreateLogFilterMessageParsing(x, isUserDefined: true);
            vm.Deleted.Subscribe(viewModel => _messageParsingCategory.RemoveItem(viewModel)).DisposeWith(vm.DisposerExposed);
            vm.Committed.Subscribe(_ =>
            {
                IsAddEditMessageParsingVisible = false;
                _filterChanged.OnNext(Unit.Default);
            }).DisposeWith(vm.DisposerExposed);
            vm.Modifying.Subscribe(editingViewModel =>
            {
                EditingMessageParsing = editingViewModel;
                IsAddEditMessageParsingVisible = true;
            }).DisposeWith(vm.DisposerExposed);
            vm.WhenChanged(x => x.IsSelected)
                .Subscribe(_ => IsAddEditMessageParsingVisible = false)
                .DisposeWith(vm.DisposerExposed);
            return vm;
        }).ToList();
        _messageParsingCategory.AddItems(vms);
        SubscribeToPinningEvents(vms, () => _filterChanged.OnNext(Unit.Default));
        return vms;
    }

    private void AddSources(IEnumerable<LogSourceBase> sources)
    {
        _sourcesCategory.AddItems(
            sources.OrderBy(x => x.DisplayName)
                .Select(x => new LogSourceViewModel(x))
        );
    }

    public IObservable<Unit> FilterChanged => _filterChanged;

    public ObservableCollection<ILogFilterNodeViewModel> FilterCategories { get; } = [];
    public ObservableCollection<ILogFilterNodeViewModel> SelectedFilters { get; } = [];

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

    public bool IsAddEditMessageParsingVisible
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public IMessageParsingViewModel? EditingMessageParsing
    {
        get => GetOrDefault<IMessageParsingViewModel?>();
        set => RaiseAndSetIfChanged(value);
    }

    public bool IsOr
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }
}
