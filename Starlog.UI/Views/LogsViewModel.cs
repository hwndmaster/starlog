using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Documents;
using DynamicData;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Starlog.Core;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.UI.AutoGridBuilders;
using Genius.Starlog.UI.Controllers;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views.LogSearchAndFiltering;

namespace Genius.Starlog.UI.Views;

public interface ILogsViewModel : ITabViewModel, IDisposable
{
    void ResetGrouping();
    void UnBookmarkAll();

    ILogsFilteringViewModel Filtering { get; }
    ILogsSearchViewModel Search { get; }
    ObservableCollection<ILogItemViewModel> SelectedLogItems { get; }
}

// TODO: Cover with unit tests
public sealed class LogsViewModel : TabViewModelBase, ILogsViewModel
{
    private readonly ICurrentProfile _currentProfile;
    private readonly ILogContainer _logContainer;
    private readonly ILogRecordMatcher _logRecordMatcher;
    private readonly ILogArtifactsFormatter _artifactsFormatter;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly CompositeDisposable _subscriptions;
    private LogRecordMatcherContext? _filterContext;
    private bool _suspendUpdate = false;

    public LogsViewModel(
        ICurrentProfile currentProfile,
        ILogContainer logContainer,
        LogItemAutoGridBuilder autoGridBuilder,
        ILogRecordMatcher logRecordMatcher,
        ILogArtifactsFormatter artifactsFormatter,
        ILogsFilteringViewModel logsFilteringViewModel,
        ILogsSearchViewModel logsSearchViewModel,
        IMainController controller,
        IUiDispatcher uiDispatcher)
    {
        Guard.NotNull(controller);

        // Dependencies:
        _currentProfile = currentProfile.NotNull();
        _logContainer = logContainer.NotNull();
        AutoGridBuilder = autoGridBuilder.NotNull();
        _logRecordMatcher = logRecordMatcher.NotNull();
        _artifactsFormatter = artifactsFormatter.NotNull();
        _uiDispatcher = uiDispatcher.NotNull();
        Filtering = logsFilteringViewModel.NotNull();
        Search = logsSearchViewModel.NotNull();

        // Members initialization:
        LogItemsView.Source = LogItems;
        LogItemsView.Filter += OnLogItemsViewFilter;

        // Actions:
        ShareCommand = new ActionCommand(async _ =>
            await controller.ShowShareViewAsync(SelectedLogItems));
        ReloadProfileCommand = new ActionCommand(async _ =>
            {
                if (_currentProfile.Profile is null) return;
                _suspendUpdate = true;
                await controller.LoadProfileAsync(_currentProfile.Profile);
            });

        // Subscriptions:
        _subscriptions = new(
            _currentProfile.ProfileClosed
                .Subscribe(_ =>
                {
                    _suspendUpdate = true;
                    _uiDispatcher.BeginInvoke(() =>
                    {
                        IsProfileReady = false;
                        IsRefreshVisible = false;
                        LogItems.Clear();
                        SelectedLogItems.Clear();
                        SelectedLogItem = null;
                    });
                }),
            _currentProfile.ProfileChanged
                .Subscribe(profile =>
                {
                    if (profile is null) return;

                    _uiDispatcher.BeginInvoke(() =>
                    {
                        AddLogs(_logContainer.GetLogs());
                        IsProfileReady = true;
                        _suspendUpdate = false;
                    });
                }),
            _currentProfile.UnknownChangesDetected
                .Subscribe(_ => IsRefreshVisible = true),
            _logContainer.LogsAdded
                .Where(_ => !_suspendUpdate)
                .Subscribe(x => _uiDispatcher.BeginInvoke(() => AddLogs(x))),
            _logContainer.LogsRemoved
                .Where(_ => !_suspendUpdate)
                .Subscribe(x => _uiDispatcher.BeginInvoke(() => RemoveLogs(x))),
            _logContainer.FileRenamed
                .Subscribe(args =>
                {
                    _uiDispatcher.BeginInvoke(() => {
                        foreach (var logItem in LogItems)
                        {
                            if (logItem.Record.File.FileName.Equals(args.OldRecord.FileName, StringComparison.Ordinal))
                            {
                                logItem.HandleFileRenamed(args.NewRecord);
                            }
                        }});
                }),
            _logContainer.FilesCountChanged
                .Subscribe(filesCount =>
                {
                    IsFileColumnVisible = filesCount > 1;
                }),
            Filtering.FilterChanged.Merge(Search.SearchChanged)
                .Subscribe(_ => RefreshFilteredItems()),
            this.WhenChanged(x => x.ColorizeBy).Subscribe(_ =>
            {
                var colorizeByThread = ColorizeBy.Equals("T", StringComparison.Ordinal);
                foreach (var logItem in LogItems)
                {
                    logItem.ColorizeByThread = colorizeByThread;
                }
            }),
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
            }),
            SelectedLogItems.WhenCollectionChanged()
                .Subscribe(_ => SelectedLogItem = SelectedLogItems.FirstOrDefault()),
            this.WhenChanged(x => x.SelectedLogItem)
                .Subscribe(_ => SelectedLogArtifacts = SelectedLogItem?.Artifacts)
        );
    }

    public void ResetGrouping()
    {
        GroupBy = string.Empty;
    }

    public void UnBookmarkAll()
    {
        foreach (var item in LogItems)
        {
            item.IsBookmarked = false;
        }

        Filtering.DropBookmarkedFilter();
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
    }

    private void AddLogs(ICollection<LogRecord> logs)
    {
        if (!logs.Any())
        {
            return;
        }

        Search.Reconcile(LogItems.Count, logs);

        using (var suppressed = LogItems.DelayNotifications())
        {
            foreach (var log in logs.OrderBy(x => x.DateTime))
            {
                suppressed.Add(new LogItemViewModel(log, _artifactsFormatter));
            }
        }

        RefreshFilteredItems();
    }

    private void RemoveLogs(ICollection<LogRecord> logs)
    {
        if (!logs.Any())
        {
            return;
        }

        using (var suppressed = LogItems.DelayNotifications())
        {
            var toRemove = LogItems.Where(x => logs.Contains(x.Record)).ToList();
            foreach (var item in toRemove)
            {
                suppressed.Remove(item);
            }
        }

        RefreshFilteredItems();
    }

    private void OnLogItemsViewFilter(object sender, FilterEventArgs e)
    {
        var viewModel = (ILogItemViewModel)e.Item;

        if (_filterContext?.Filter.ShowBookmarked == true)
        {
            e.Accepted = viewModel.IsBookmarked;
            return;
        }

        e.Accepted = _logRecordMatcher.IsMatch(_filterContext, viewModel.Record);
    }

    private void RefreshFilteredItems()
    {
        _filterContext = new LogRecordMatcherContext(
            Filtering.CreateContext(),
            Search.CreateContext()
        );

        _uiDispatcher.BeginInvoke(() =>
        {
            SearchText = Search.Text;
            SearchUseRegex = Search.UseRegex;

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

    public bool IsProfileReady
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public bool IsRefreshVisible
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public ILogsFilteringViewModel Filtering { get; }

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

    public IAutoGridBuilder AutoGridBuilder { get; }

    public DelayedObservableCollection<ILogItemViewModel> LogItems { get; }
        = new TypedObservableCollection<ILogItemViewModel, LogItemViewModel>();

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

    public bool AutoScroll
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public int StatsFilteredCount
    {
        get => GetOrDefault<int>();
        set => RaiseAndSetIfChanged(value);
    }

    public bool IsFileColumnVisible
    {
        get => GetOrDefault(true);
        set => RaiseAndSetIfChanged(value);
    }

    /// <summary>
    ///   Used for binding into DataGrid's text highlighting.
    /// </summary>
    public string SearchText
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    /// <summary>
    ///   Used for binding into DataGrid's text highlighting.
    /// </summary>
    public bool SearchUseRegex
    {
        get => GetOrDefault<bool>();
        set => RaiseAndSetIfChanged(value);
    }

    public IActionCommand ShareCommand { get; }
    public IActionCommand ReloadProfileCommand { get; }
}
