using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Documents;
using Genius.Atom.UI.Forms;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.UI.AutoGridBuilders;
using Genius.Starlog.UI.Helpers;

namespace Genius.Starlog.UI.ViewModels;

public interface ILogsViewModel : ITabViewModel
{
}

public sealed class LogsViewModel : TabViewModelBase, ILogsViewModel
{
    private readonly ILogContainer _logContainer;
    private readonly ILogFiltersHelper _logFiltersHelper;
    private readonly LogFilterCategoryViewModel<LogFileViewModel> _filesCategory = new("Files", "FolderFiles32", sort: true);
    private readonly LogFilterCategoryViewModel<LogFilterViewModel> _quickFiltersCategory = new("Quick filters", "FolderDown32", expanded: true);
    private readonly LogFilterCategoryViewModel<LogFilterViewModel> _userFiltersCategory = new("User filters", "FolderFavs32");
    private readonly CompositeDisposable _subscriptions = new();
    private bool _suspendUpdate = false;
    private LogFilterContext? _filterContext;

    public LogsViewModel(ILogContainer logContainer,
        LogItemAutoGridBuilder autoGridBuilder,
        ILogFiltersHelper logFiltersHelper)
    {
        _logContainer = logContainer.NotNull();
        AutoGridBuilder = autoGridBuilder.NotNull();
        _logFiltersHelper = logFiltersHelper.NotNull();

        LogItemsView.Source = LogItems;
        LogItemsView.Filter += OnLogItemsViewFilter;

        _logFiltersHelper.InitializeQuickFiltersCategory(_quickFiltersCategory);

        _subscriptions.Add(_logContainer.ProfileChanging
            //.ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                _suspendUpdate = true;
                _filesCategory.CategoryItems.Clear();
                _filesCategory.CategoryItemsView.View.Refresh();
                _userFiltersCategory.CategoryItems.Clear();
                LogItems.Clear();
            }));
        _subscriptions.Add(_logContainer.ProfileChanged
            .Subscribe(_ =>
            {
                AddFiles(_logContainer.GetFiles());
                AddLogs(_logContainer.GetLogs());
                _suspendUpdate = false;
            }));
        _subscriptions.Add(_logContainer.FileAdded
            .Where(_ => !_suspendUpdate)
            .Subscribe(x => AddFiles(new [] { x })));
        _subscriptions.Add(_logContainer.LogsAdded
            .Where(_ => !_suspendUpdate)
            .Subscribe(x => AddLogs(x)));

        ShareCommand = new ActionCommand(_ => throw new NotImplementedException());
        SearchRegexSwitchCommand = new ActionCommand(_ =>
        {
            SearchRegex = !SearchRegex;
            if (!string.IsNullOrWhiteSpace(SearchText))
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

        this.WhenChanged(x => x.SearchText).Subscribe(_ => RefreshFilteredItems());
        SelectedFilters.WhenCollectionChanged().Subscribe(_ => RefreshFilteredItems());
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

    private void AddLogs(IEnumerable<LogRecord> logs)
    {
        foreach (var log in logs.OrderBy(x => x.DateTime))
        {
            LogItems.Add(new LogItemViewModel(log));
        }
    }

    private void OnLogItemsViewFilter(object sender, FilterEventArgs e)
    {
        e.Accepted = _logFiltersHelper.IsMatch(_filterContext, (LogItemViewModel)e.Item);
    }

    private void RefreshFilteredItems()
    {
        _filterContext = _logFiltersHelper.CreateContext(SelectedFilters, SearchText, SearchRegex);
        LogItemsView.View.Refresh();
    }

    public string SearchText
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public bool SearchRegex
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public string ColorizeBy
    {
        get => GetOrDefault("L");
        set => RaiseAndSetIfChanged(value);
    }

    public IAutoGridBuilder AutoGridBuilder { get; }

    public ObservableCollection<ILogFilterCategoryViewModel> FilterCategories { get; } = new();
    public ObservableCollection<ILogFilterCategoryViewModel> SelectedFilters { get; } = new();

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

    public IActionCommand ShareCommand { get; }
    public IActionCommand SearchRegexSwitchCommand { get; }
}
