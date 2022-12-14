using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Documents;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.UI.Forms;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.AutoGridBuilders;
using Genius.Starlog.UI.Helpers;

namespace Genius.Starlog.UI.ViewModels;

public interface ILogsViewModel : ITabViewModel
{
}

public sealed class LogsViewModel : TabViewModelBase, ILogsViewModel
{
    private readonly ICommandBus _commandBus;
    private readonly ICurrentProfile _currentProfile;
    private readonly ILogContainer _logContainer;
    private readonly ILogFiltersHelper _logFiltersHelper;
    private readonly ILogArtifactsFormatter _artifactsFormatter;
    private readonly IViewModelFactory _vmFactory;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly IUserInteraction _ui;
    private readonly LogFilterCategoryViewModel<LogFileViewModel> _filesCategory = new("Files", "FolderFiles32", sort: true);
    private readonly LogFilterCategoryViewModel<LogFilterViewModel> _quickFiltersCategory = new("Quick filters", "FolderDown32", expanded: true);
    private readonly LogFilterCategoryViewModel<LogFilterViewModel> _userFiltersCategory = new("User filters", "FolderFavs32", expanded: true, canAddChildren: true);
    private readonly CompositeDisposable _subscriptions = new();
    private bool _suspendUpdate = false;
    private LogFilterContext? _filterContext;

    public LogsViewModel(
        ICommandBus commandBus,
        ICurrentProfile currentProfile,
        ILogContainer logContainer,
        LogItemAutoGridBuilder autoGridBuilder,
        ILogFiltersHelper logFiltersHelper,
        ILogArtifactsFormatter artifactsFormatter,
        IViewModelFactory vmFactory,
        IUiDispatcher uiDispatcher,
        IUserInteraction ui)
    {
        _commandBus = commandBus.NotNull();
        _currentProfile = currentProfile.NotNull();
        _logContainer = logContainer.NotNull();
        AutoGridBuilder = autoGridBuilder.NotNull();
        _logFiltersHelper = logFiltersHelper.NotNull();
        _artifactsFormatter = artifactsFormatter.NotNull();
        _vmFactory = vmFactory.NotNull();
        _uiDispatcher = uiDispatcher.NotNull();
        _ui = ui.NotNull();

        Search = vmFactory.CreateLogSearch();

        LogItemsView.Source = LogItems;
        LogItemsView.Filter += OnLogItemsViewFilter;

        _logFiltersHelper.InitializeQuickFiltersCategory(_quickFiltersCategory);
        _logFiltersHelper.InitializePinSubscription(_quickFiltersCategory.CategoryItems, () => RefreshFilteredItems());
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

        _subscriptions.Add(_currentProfile.ProfileChanging
            //.ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                _suspendUpdate = true;

                _uiDispatcher.BeginInvoke(() =>
                {
                    _filesCategory.CategoryItems.Clear();
                    _filesCategory.CategoryItemsView.View.Refresh();
                    _userFiltersCategory.CategoryItems.Clear();
                    LogItems.Clear();
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
                    AddLogs(_logContainer.GetLogs());
                    _suspendUpdate = false;
                });
            }));
        _subscriptions.Add(_logContainer.FileAdded
            .Where(_ => !_suspendUpdate)
            .Subscribe(x => AddFiles(new [] { x })));
        _subscriptions.Add(_logContainer.LogsAdded
            .Where(_ => !_suspendUpdate)
            .Subscribe(x => AddLogs(x)));

        ShareCommand = new ActionCommand(_ =>
        {
            // TODO: Implement sharing
            throw new NotImplementedException();
        });
        SearchRegexSwitchCommand = new ActionCommand(_ =>
        {
            Search.UseRegex = !Search.UseRegex;
            if (!string.IsNullOrWhiteSpace(Search.Text))
            {
                RefreshFilteredItems();
            }
        });

        FilterCategories.Add(_filesCategory);
        FilterCategories.Add(_quickFiltersCategory);
        FilterCategories.Add(_userFiltersCategory);

        this.WhenChanged(x => x.ColorizeBy).Subscribe(_ =>
        {
            var colorizeByThread = ColorizeBy.Equals("T", StringComparison.Ordinal);
            foreach (var logItem in LogItems)
            {
                logItem.ColorizeByThread = colorizeByThread;
            }
        });

        this.WhenChanged(x => x.GroupBy).Subscribe(_ =>
        {
            LogItemsView.GroupDescriptions.Clear();

            if (string.IsNullOrEmpty(GroupBy))
            {
                return;
            }

            var propertyName = GroupBy switch
            {
                "M" => nameof(ILogItemViewModel.Message),
                // TODO: Implement fuzzy grouping
                "MF" => nameof(ILogItemViewModel.Message),
                "L" => nameof(ILogItemViewModel.Logger),
                _ => throw new NotSupportedException($"Field ID '{GroupBy}' is unknown.")
            };

            LogItemsView.GroupDescriptions.Add(new PropertyGroupDescription(propertyName));
        });

        Search.WhenAnyChanged(x => x.Text, x => x.SelectedDateTimeFromTicks, x => x.SelectedDateTimeToTicks)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(_ => RefreshFilteredItems());
        SelectedFilters.WhenCollectionChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(_ => RefreshFilteredItems());
        SelectedLogItems.WhenCollectionChanged().Subscribe(_ => SelectedLogItem = SelectedLogItems.FirstOrDefault());
        this.WhenChanged(x => x.SelectedLogItem).Subscribe(_ => SelectedLogArtifacts = SelectedLogItem?.Artifacts);
    }

    private void AddFiles(IEnumerable<FileRecord> files)
    {
        _filesCategory.AddItems(
            files.OrderBy(x => x.FileName)
                .Select(x => new LogFileViewModel(x))
        );
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
                        RefreshFilteredItems();
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
        _logFiltersHelper.InitializePinSubscription(vms, () => RefreshFilteredItems());
        return vms;
    }

    private void AddLogs(ICollection<LogRecord> logs)
    {
        if (!logs.Any())
        {
            return;
        }

        Search.Reconcile(LogItems.Count, logs);

        foreach (var log in logs.OrderBy(x => x.DateTime))
        {
            LogItems.Add(new LogItemViewModel(log, _artifactsFormatter));
        }

        RefreshFilteredItems();
    }

    private void OnLogItemsViewFilter(object sender, FilterEventArgs e)
    {
        e.Accepted = _logFiltersHelper.IsMatch(_filterContext, (LogItemViewModel)e.Item);
    }

    private void RefreshFilteredItems()
    {
        var filters = SelectedFilters
            .Union(_filesCategory.CategoryItems.Where(x => x.IsPinned))
            .Union(_quickFiltersCategory.CategoryItems.Where(x => x.IsPinned))
            .Union(_userFiltersCategory.CategoryItems.Where(x => x.IsPinned));
        _filterContext = _logFiltersHelper.CreateContext(filters, Search);

        _uiDispatcher.BeginInvoke(() =>
        {
            LogItemsView.View.Refresh();

            var filteredItems = LogItemsView.View.Cast<ILogItemViewModel>().ToList();
            StatsFilteredCount = filteredItems.Count;

            // TODO: Filter out items by log level and show stats like:
            //       TRACE:  xxx
            //       INFO:    kk
            //       WARN:   yyy
            //       ERROR:    z
        });
    }

    public ILogsSearchViewModel Search { get; }

    public string ColorizeBy
    {
        get => GetOrDefault("L");
        set => RaiseAndSetIfChanged(value);
    }

    public string GroupBy
    {
        get => GetOrDefault(string.Empty);
        set => RaiseAndSetIfChanged(value);
    }

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

    public IAutoGridBuilder AutoGridBuilder { get; }

    public ObservableCollection<ILogFilterNodeViewModel> FilterCategories { get; } = new();
    public ObservableCollection<ILogFilterNodeViewModel> SelectedFilters { get; } = new();

    public ObservableCollection<ILogItemViewModel> LogItems { get; }
        = new TypedObservableList<ILogItemViewModel, LogItemViewModel>();

    public CollectionViewSource LogItemsView { get; } = new();

    public ObservableCollection<ILogItemViewModel> SelectedLogItems { get; set; } = new();

    public ILogItemViewModel? SelectedLogItem
    {
        get => GetOrDefault<ILogItemViewModel>();
        set => RaiseAndSetIfChanged(value);
    }

    public FlowDocument? SelectedLogArtifacts
    {
        get => GetOrDefault<FlowDocument>();
        set => RaiseAndSetIfChanged(value);
    }

    public int StatsFilteredCount
    {
        get => GetOrDefault<int>();
        set => RaiseAndSetIfChanged(value);
    }

    public IActionCommand ShareCommand { get; }
    public IActionCommand SearchRegexSwitchCommand { get; }
}
