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
    private readonly ICurrentProfile _currentProfile;
    private readonly ILogContainer _logContainer;
    private readonly ILogFiltersHelper _logFiltersHelper;
    private readonly ILogArtifactsFormatter _artifactsFormatter;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly CompositeDisposable _subscriptions = new(); // TODO: Dispose this
    private LogOverallFilterContext? _filterContext;
    private bool _suspendUpdate = false;

    public LogsViewModel(
        ICurrentProfile currentProfile,
        ILogContainer logContainer,
        LogItemAutoGridBuilder autoGridBuilder,
        ILogFiltersHelper logFiltersHelper,
        ILogArtifactsFormatter artifactsFormatter,
        ILogsFilteringViewModel logsFilteringViewModel,
        ILogsSearchViewModel logsSearchViewModel,
        IUiDispatcher uiDispatcher)
    {
        _currentProfile = currentProfile.NotNull();
        _logContainer = logContainer.NotNull();
        AutoGridBuilder = autoGridBuilder.NotNull();
        _logFiltersHelper = logFiltersHelper.NotNull();
        _artifactsFormatter = artifactsFormatter.NotNull();
        _uiDispatcher = uiDispatcher.NotNull();

        Filtering = logsFilteringViewModel.NotNull();
        Search = logsSearchViewModel.NotNull();

        Filtering.FilterChanged.Concat(Search.SearchChanged).Subscribe(_ =>
            RefreshFilteredItems());

        LogItemsView.Source = LogItems;
        LogItemsView.Filter += OnLogItemsViewFilter;

        _subscriptions.Add(_currentProfile.ProfileChanging
            .Subscribe(_ =>
            {
                _suspendUpdate = true;
                _uiDispatcher.BeginInvoke(() => LogItems.Clear());
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
                    AddLogs(_logContainer.GetLogs());
                    _suspendUpdate = false;
                });
            }));
        _subscriptions.Add(_logContainer.LogsAdded
            .Where(_ => !_suspendUpdate)
            .Subscribe(x => AddLogs(x)));

        ShareCommand = new ActionCommand(_ =>
        {
            // TODO: Implement sharing
            throw new NotImplementedException();
        });

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

        SelectedLogItems.WhenCollectionChanged().Subscribe(_ => SelectedLogItem = SelectedLogItems.FirstOrDefault());
        this.WhenChanged(x => x.SelectedLogItem).Subscribe(_ => SelectedLogArtifacts = SelectedLogItem?.Artifacts);
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
        e.Accepted = _logFiltersHelper.IsMatch(_filterContext, (ILogItemViewModel)e.Item);
    }

    private void RefreshFilteredItems()
    {
        _filterContext = new LogOverallFilterContext(
            Filtering.CreateContext(),
            Search.CreateContext()
        );

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

    public ILogsFilteringViewModel Filtering { get; }

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

    public IAutoGridBuilder AutoGridBuilder { get; }

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
}
