using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Documents;
using DynamicData;
using Genius.Atom.Infrastructure.Tasks;
using Genius.Atom.UI.Forms.Controls.AutoGrid;
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
    DelayedObservableCollection<ILogItemViewModel> LogItems { get; }
    ILogsSearchViewModel Search { get; }
    ObservableCollection<ILogItemViewModel> SelectedLogItems { get; }
}

public sealed class LogsViewModel : TabViewModelBase, ILogsViewModel
{
    public record ColorizeByRecord(string Title, bool ForField, int? FieldId);
    public record GroupByRecord(string Title, LogItemGroupingOptions GroupingOption, int? FieldId);

    private readonly ICurrentProfile _currentProfile;
    private readonly ILogContainer _logContainer;
    private readonly ILogRecordMatcher _logRecordMatcher;
    private readonly ILogArtifactsFormatter _artifactsFormatter;
    private readonly MessageParsingHelper _messageParsingHelper;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly Disposer _subscriptions = new();
    private readonly int _predefinedGroupByOptionsCount;
    private LogRecordMatcherContext? _filterContext;
    private bool _profileLoadingUpdateSuspended;
    private bool _refreshFilteredItemsSuspended;
    private bool _isReloadingProfile;

    public LogsViewModel(
        ICurrentProfile currentProfile,
        ILogContainer logContainer,
        LogItemAutoGridBuilder autoGridBuilder,
        ILogRecordMatcher logRecordMatcher,
        ILogArtifactsFormatter artifactsFormatter,
        ILogsFilteringViewModel logsFilteringViewModel,
        ILogsSearchViewModel logsSearchViewModel,
        IMainController controller,
        MessageParsingHelper messageParsingHelper,
        IProfileLoadingController profileLoadingController,
        IUiDispatcher uiDispatcher)
    {
        Guard.NotNull(controller);
        Guard.NotNull(profileLoadingController);

        // Dependencies:
        AutoGridBuilder = autoGridBuilder.NotNull();
        _artifactsFormatter = artifactsFormatter.NotNull();
        _currentProfile = currentProfile.NotNull();
        _logContainer = logContainer.NotNull();
        _logRecordMatcher = logRecordMatcher.NotNull();
        _messageParsingHelper = messageParsingHelper.NotNull();
        _uiDispatcher = uiDispatcher.NotNull();
        Filtering = logsFilteringViewModel.NotNull();
        Search = logsSearchViewModel.NotNull();

        // Members initialization:
        LogItemsView.Source = LogItems;
        LogItemsView.Filter += OnLogItemsViewFilter;
        ColorizeByOptions.Add(new ColorizeByRecord("Colorize by Level", false, null));
        ColorizeBy = ColorizeByOptions[0];
        GroupByOptions.Add(new GroupByRecord("No grouping", LogItemGroupingOptions.NoGrouping, null));
        GroupByOptions.Add(new GroupByRecord("Group by messages", LogItemGroupingOptions.ByMessage, null));
        GroupByOptions.Add(new GroupByRecord("Group by messages (omit digits)", LogItemGroupingOptions.ByMessageTrimmed, null));
        _predefinedGroupByOptionsCount = GroupByOptions.Count;
        ResetGrouping();

        // Actions:
        ShareCommand = new ActionCommand(async _ =>
            await controller.ShowShareViewAsync(SelectedLogItems));
        ReloadProfileCommand = new ActionCommand(async _ =>
            {
                if (_currentProfile.Profile is null)
                    return;
                _profileLoadingUpdateSuspended = true;
                _isReloadingProfile = true;
                Filtering.SuspendResumeProfileReload(true);
                await profileLoadingController.LoadProfileAsync(_currentProfile.Profile);
                _isReloadingProfile = false;
                Filtering.SuspendResumeProfileReload(false);
            });

        // Subscriptions:
        _currentProfile.ProfileClosed
            .Subscribe(_ =>
            {
                _profileLoadingUpdateSuspended = true;
                _uiDispatcher.Invoke(() =>
                {
                    IsProfileReady = false;
                    IsRefreshVisible = false;
                    LogItems.Clear();
                    SelectedLogItems.Clear();
                    SelectedLogItem = null;

                    if (_isReloadingProfile)
                        return;

                    Search.DropAllSearches();
                });
            }).DisposeWith(_subscriptions);
        _currentProfile.ProfileChanged
            .Subscribe(profile =>
            {
                if (profile is null) return;

                var fieldsContainer = _logContainer.GetFields();
                var fields = fieldsContainer.GetFields();
                var fieldNames = fields.Select(x => x.FieldName).ToArray();

                _uiDispatcher.Invoke(() =>
                {
                    AddLogs(_logContainer.GetLogs());
                    FieldColumns = new DynamicColumnsViewModel(fieldNames);

                    if (!_isReloadingProfile)
                    {
                        ColorizeBy = ColorizeByOptions[0];
                        while (ColorizeByOptions.Count > 1)
                            ColorizeByOptions.RemoveAt(1);
                        foreach (var field in fields)
                            ColorizeByOptions.Add(new ColorizeByRecord("Colorize by " + field.FieldName, true, field.FieldId));

                        ResetGrouping();
                        while (GroupByOptions.Count > _predefinedGroupByOptionsCount)
                            GroupByOptions.RemoveAt(_predefinedGroupByOptionsCount);
                        foreach (var (fieldId, fieldName) in fields)
                            GroupByOptions.Add(new GroupByRecord("Group by " + fieldName, LogItemGroupingOptions.ByField, fieldId));
                    }
                    else
                    {
                        RefreshGrouping();
                    }

                    IsProfileReady = true;
                    _profileLoadingUpdateSuspended = false;
                });
            }).DisposeWith(_subscriptions);
        _currentProfile.UnknownChangesDetected
            .Subscribe(_ => IsRefreshVisible = true)
            .DisposeWith(_subscriptions);
        _logContainer.LogsAdded
            .Where(_ => !_profileLoadingUpdateSuspended)
            .Subscribe(x => AddLogs(x))
            .DisposeWith(_subscriptions);
        _logContainer.LogsRemoved
            .Where(_ => !_profileLoadingUpdateSuspended)
            .SubscribeOnUiThread(x => RemoveLogs(x))
            .DisposeWith(_subscriptions);
        _logContainer.SourceRenamed
            .SubscribeOnUiThread(args =>
            {
                foreach (var logItem in LogItems)
                {
                    if (logItem.Record.Source.DisplayName.Equals(args.OldRecord.DisplayName, StringComparison.Ordinal))
                    {
                        logItem.HandleSourceRenamed(args.NewRecord);
                    }
                }
            }).DisposeWith(_subscriptions);
        _logContainer.SourcesCountChanged
            .Subscribe(sourcesCount =>
            {
                IsFileColumnVisible = sourcesCount > 1;
            }).DisposeWith(_subscriptions);
        Filtering.FilterChanged.Merge(Search.SearchChanged)
            .Subscribe(_ => RefreshFilteredItems())
            .DisposeWith(_subscriptions);
        this.WhenChanged(x => x.ColorizeBy).Subscribe(_ =>
        {
            foreach (var logItem in LogItems)
            {
                logItem.ColorizeByFieldId = ColorizeBy.ForField ? ColorizeBy.FieldId : null;
                logItem.ColorizeByField = ColorizeBy.ForField;
            }
        }).DisposeWith(_subscriptions);
        this.WhenChanged(x => x.GroupBy)
            .Subscribe(_ => RefreshGrouping())
            .DisposeWith(_subscriptions);
        SelectedLogItems.WhenCollectionChanged()
            .Subscribe(_ => SelectedLogItem = SelectedLogItems.FirstOrDefault())
            .DisposeWith(_subscriptions);
        this.WhenChanged(x => x.SelectedLogItem)
            .Subscribe(_ => SelectedLogArtifacts = SelectedLogItem?.Artifacts)
            .DisposeWith(_subscriptions);
    }

    private void RefreshGrouping()
    {
        var doGrouping = GroupBy.GroupingOption != LogItemGroupingOptions.NoGrouping;
        if (doGrouping)
        {
            var groups = LogItems.GroupBy(x => x.GetGroupValueId(GroupBy.GroupingOption, GroupBy.FieldId));
            foreach (var group in groups)
            {
                var groupableVm = group.First().CreateGrouping(GroupBy.GroupingOption, GroupBy.FieldId);
                foreach (var item in group)
                {
                    item.GroupableField = groupableVm;
                }
            }
        }

        if (DoGrouping == doGrouping)
        {
            OnPropertyChanged(nameof(DoGrouping));
        }
        else
        {
            DoGrouping = doGrouping;
        }
    }

    public void ResetGrouping()
    {
        GroupBy = GroupByOptions[0];
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
        if (logs.Count == 0)
        {
            return;
        }

        _refreshFilteredItemsSuspended = true;

        var resetSelected = !_isReloadingProfile
            && LogItems.Count == 0;
        Search.Reconcile(resetSelected, logs);

        var fields = _logContainer.GetFields();
        var hasFields = fields.GetFieldCount() > 0;

        List<LogItemViewModel> viewModelsToAdd = [];
        foreach (var log in logs.OrderBy(x => x.DateTime))
        {
            var logItemVm = new LogItemViewModel(_logContainer, log, _artifactsFormatter);

            if (hasFields)
            {
                logItemVm.FieldEntries = new DynamicColumnEntriesViewModel(() =>
                {
                    string[] fieldValues = new string[log.FieldValueIndices.Length];
                    for (var fieldId = 0; fieldId < log.FieldValueIndices.Length; fieldId++)
                    {
                        fieldValues[fieldId] = fields.GetFieldValue(fieldId, log.FieldValueIndices[fieldId]);
                    }
                    return fieldValues;
                });
            }

            viewModelsToAdd.Add(logItemVm);
        }

        _uiDispatcher.Invoke(() =>
        {
            using (var suppressed = LogItems.DelayNotifications())
            {
                foreach (var logItemVm in viewModelsToAdd)
                {
                    suppressed.Add(logItemVm);
                }
            }
        });

        _refreshFilteredItemsSuspended = false;
        RefreshFilteredItems();
    }

    private void RemoveLogs(ICollection<LogRecord> logs)
    {
        if (logs.Count == 0)
            return;

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
        if (_refreshFilteredItemsSuspended)
        {
            return;
        }

        _filterContext = new LogRecordMatcherContext(
            Filtering.CreateContext(),
            Search.CreateContext()
        );

        // `MessageParsingColumns` needs to be updated in a UI thread to avoid WPF binding errors.
        DynamicColumnsViewModel? messageParsingColumnsToSet =
            _filterContext.Filter.MessageParsings.Length > 0 || MessageParsingColumns is not null
                ? _messageParsingHelper.CreateDynamicMessageParsingEntries(_filterContext.Filter, LogItems)
                : null;

        _uiDispatcher.InvokeAsync(() =>
        {
            if (messageParsingColumnsToSet is not null)
            {
                MessageParsingColumns = messageParsingColumnsToSet;
            }

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
        }).RunAndForget();
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

    public ColorizeByRecord ColorizeBy
    {
        get => GetOrDefault<ColorizeByRecord>();
        set => RaiseAndSetIfChanged(value);
    }

    public ObservableCollection<ColorizeByRecord> ColorizeByOptions { get; } = new();

    public bool DoGrouping
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public GroupByRecord GroupBy
    {
        get => GetOrDefault<GroupByRecord>();
        set => RaiseAndSetIfChanged(value);
    }

    public ObservableCollection<GroupByRecord> GroupByOptions { get; } = new();

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

    public DynamicColumnsViewModel? FieldColumns
    {
        get => GetOrDefault<DynamicColumnsViewModel?>();
        set => RaiseAndSetIfChanged(value);
    }

    public DynamicColumnsViewModel? MessageParsingColumns
    {
        get => GetOrDefault<DynamicColumnsViewModel?>();
        set => RaiseAndSetIfChanged(value);
    }

    public IActionCommand ShareCommand { get; }
    public IActionCommand ReloadProfileCommand { get; }
}
