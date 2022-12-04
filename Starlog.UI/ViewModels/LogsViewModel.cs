using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;
using Genius.Atom.UI.Forms;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.UI.AutoGridBuilders;
using ReactiveUI;

namespace Genius.Starlog.UI.ViewModels;

public interface ILogsViewModel : ITabViewModel
{
}

public record LogFilterContext(
    bool MessageFilterSpecified,
    Regex? MessageFilterRegex,
    HashSet<string> FilesSelected);

public sealed class LogsViewModel : TabViewModelBase, ILogsViewModel
{
    private readonly ILogContainer _logContainer;
    private readonly LogFilterCategoryViewModel<LogFileViewModel> _filesCategory = new("Files", "FolderFiles32", sort: true);
    private readonly LogFilterCategoryViewModel<LogFilterViewModel> _quickFiltersCategory = new("Quick filters", "FolderDown32");
    private readonly LogFilterCategoryViewModel<LogFilterViewModel> _userFiltersCategory = new("User filters", "FolderFavs32");
    private readonly CompositeDisposable _subscriptions = new();
    private bool _suspendUpdate = false;
    private LogFilterContext? _filterContext;

    public LogsViewModel(ILogContainer logContainer,
        LogItemAutoGridBuilder autoGridBuilder)
    {
        _logContainer = logContainer.NotNull();
        AutoGridBuilder = autoGridBuilder.NotNull();

        LogItemsView.Source = LogItems;
        LogItemsView.Filter += OnLogItemsViewFilter;

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
        FilterRegexSwitchCommand = new ActionCommand(_ =>
        {
            FilterRegex = !FilterRegex;
            if (!string.IsNullOrWhiteSpace(Filter))
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

        this.WhenChanged(x => x.Filter).Subscribe(_ => RefreshFilteredItems());
    }

    private void AddFiles(IEnumerable<FileRecord> files)
    {
        foreach (var file in files.OrderBy(x => x.FileName))
        {
            var vm = new LogFileViewModel(file);
            vm.WhenChanged(x => x.IsSelected).Subscribe(_ => RefreshFilteredItems());
            _filesCategory.CategoryItems.Add(vm);
        }
        _filesCategory.CategoryItemsView.View.Refresh();
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
        if (_filterContext is null)
        {
            e.Accepted = true;
            return;
        }

        var item = (LogItemViewModel)e.Item;
        if (_filterContext.FilesSelected.Count > 0
            && !_filterContext.FilesSelected.Contains(item.Record.File.FileName))
        {
            e.Accepted = false;
            return;
        }

        if (_filterContext.MessageFilterSpecified)
        {
            bool filterMatch;
            if (_filterContext.MessageFilterRegex is not null)
            {
                filterMatch = _filterContext.MessageFilterRegex.IsMatch(item.Message);
            }
            else
            {
                filterMatch = item.Message.Contains(Filter, StringComparison.InvariantCultureIgnoreCase);
            }

            if (!filterMatch)
            {
                e.Accepted = false;
                return;
            }
        }

        // TODO: Add other filters here

        e.Accepted = true;
    }

    private void RefreshFilteredItems()
    {
        var filesSelected = _filesCategory.CategoryItems
            .Where(x => x.IsSelected)
            .Select(x => x.File.FileName)
            .ToHashSet();
        var messageFilterSpecified = !string.IsNullOrWhiteSpace(Filter);

        Regex? filterRegex = null;
        if (FilterRegex)
        {
            try
            {
                filterRegex = new Regex(Filter, RegexOptions.IgnoreCase);
            }
            catch (Exception)
            {
                filterRegex = null;
            }
        }

        _filterContext = new(messageFilterSpecified, filterRegex, filesSelected);
        LogItemsView.View.Refresh();
    }

    public string Filter
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public bool FilterRegex
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

    public ObservableCollection<ILogItemViewModel> LogItems { get; }
        = new TypedObservableList<ILogItemViewModel, LogItemViewModel>();

    public CollectionViewSource LogItemsView { get; } = new();

    public IActionCommand ShareCommand { get; }
    public IActionCommand FilterRegexSwitchCommand { get; }
}
