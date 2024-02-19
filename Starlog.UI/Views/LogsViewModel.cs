using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Documents;
using DynamicData;
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
    ILogsSearchViewModel Search { get; }
    ObservableCollection<ILogItemViewModel> SelectedLogItems { get; }
}

// TODO: Cover with unit tests
public sealed class LogsViewModel : TabViewModelBase, ILogsViewModel
{
    public record ColorizeByRecord(string Title, bool ForField, int? FieldId);
    public record GroupByRecord(string Title, string? PropertyName, bool ForField, int? FieldId);

    private readonly ICurrentProfile _currentProfile;
    private readonly ILogContainer _logContainer;
    private readonly ILogRecordMatcher _logRecordMatcher;
    private readonly ILogArtifactsFormatter _artifactsFormatter;
    private readonly IMessageParsingHandler _messageParsingHandler;
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
        IMessageParsingHandler messageParsingHandler,
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
        _messageParsingHandler = messageParsingHandler.NotNull();
        _uiDispatcher = uiDispatcher.NotNull();
        Filtering = logsFilteringViewModel.NotNull();
        Search = logsSearchViewModel.NotNull();

        // Members initialization:
        LogItemsView.Source = LogItems;
        LogItemsView.Filter += OnLogItemsViewFilter;
        ColorizeByOptions.Add(new ColorizeByRecord("Colorize by Level", false, null));
        ColorizeBy = ColorizeByOptions[0];
        GroupByOptions.Add(new GroupByRecord("No grouping", null, false, null));
        GroupByOptions.Add(new GroupByRecord("Group by messages", nameof(ILogItemViewModel.Message), false, null));
        ResetGrouping();

        // Actions:
        ShareCommand = new ActionCommand(async _ =>
            await controller.ShowShareViewAsync(SelectedLogItems));
        ReloadProfileCommand = new ActionCommand(async _ =>
            {
                if (_currentProfile.Profile is null) return;
                _suspendUpdate = true;
                await profileLoadingController.LoadProfileAsync(_currentProfile.Profile);
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
                        Search.DropAllSearches();
                    });
                }),
            _currentProfile.ProfileChanged
                .Subscribe(profile =>
                {
                    if (profile is null) return;

                    var fieldsContainer = _logContainer.GetFields();
                    var fields = fieldsContainer.GetFields();
                    var fieldNames = fields.Select(x => x.FieldName).ToArray();

                    _uiDispatcher.BeginInvoke(() =>
                    {
                        AddLogs(_logContainer.GetLogs());
                        FieldColumns = new DynamicColumnsViewModel(fieldNames);

                        while (ColorizeByOptions.Count > 1)
                            ColorizeByOptions.RemoveAt(1);
                        foreach (var field in fields)
                            ColorizeByOptions.Add(new ColorizeByRecord("Colorize by " + field.FieldName, true, field.FieldId));

                        while (GroupByOptions.Count > 1)
                            GroupByOptions.RemoveAt(1);
                        foreach (var field in fields)
                            GroupByOptions.Add(new GroupByRecord("Group by " + field.FieldName, nameof(ILogItemViewModel.GroupedFieldValue), true, field.FieldId));

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
            _logContainer.SourceRenamed
                .Subscribe(args =>
                {
                    _uiDispatcher.BeginInvoke(() => {
                        foreach (var logItem in LogItems)
                        {
                            if (logItem.Record.Source.DisplayName.Equals(args.OldRecord.DisplayName, StringComparison.Ordinal))
                            {
                                logItem.HandleSourceRenamed(args.NewRecord);
                            }
                        }});
                }),
            _logContainer.SourcesCountChanged
                .Subscribe(sourcesCount =>
                {
                    IsFileColumnVisible = sourcesCount > 1;
                }),
            Filtering.FilterChanged.Merge(Search.SearchChanged)
                .Subscribe(_ => RefreshFilteredItems()),
            this.WhenChanged(x => x.ColorizeBy).Subscribe(_ =>
            {
                foreach (var logItem in LogItems)
                {
                    logItem.ColorizeByFieldId = ColorizeBy.ForField ? ColorizeBy.FieldId : null;
                    logItem.ColorizeByField = ColorizeBy.ForField;
                }
            }),
            this.WhenChanged(x => x.GroupBy).Subscribe(_ =>
            {
                LogItemsView.GroupDescriptions.Clear();

                if (GroupBy.PropertyName is null)
                    return;

                if (GroupBy.ForField) // Group by field
                {
                    foreach (var logItemVm in LogItems)
                        logItemVm.GroupedFieldId = GroupBy.FieldId;
                }

                LogItemsView.GroupDescriptions.Add(new PropertyGroupDescription(GroupBy.PropertyName));
            }),
            SelectedLogItems.WhenCollectionChanged()
                .Subscribe(_ => SelectedLogItem = SelectedLogItems.FirstOrDefault()),
            this.WhenChanged(x => x.SelectedLogItem)
                .Subscribe(_ => SelectedLogArtifacts = SelectedLogItem?.Artifacts)
        );
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
        if (!logs.Any())
        {
            return;
        }

        Search.Reconcile(LogItems.Count, logs);

        var fields = _logContainer.GetFields();
        var hasFields = fields.GetFieldCount() > 0;

        using (var suppressed = LogItems.DelayNotifications())
        {
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

                suppressed.Add(logItemVm);
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

        // `MessageParsingColumns` needs to be updated in a UI thread to avoid WPF binding errors.
        DynamicColumnsViewModel? messageParsingColumnsToSet = null;

        // TODO: Cover with unit tests
        if (_filterContext.Filter.MessageParsings.Length > 0
            || MessageParsingColumns is not null)
        {
            // TODO: Check if there were changes in `_filterContext.Filter.MessageParsings`
            //       to prevent re-initialization in case of no changes.

            var messageParsings = _filterContext.Filter.MessageParsings;
            var extractedColumns = (from messageParsing in messageParsings
                                    from extractedColumn in _messageParsingHandler.RetrieveColumns(messageParsing)
                                    select (messageParsing, extractedColumn)).ToArray();

            foreach (var logItem in LogItems)
            {
                logItem.MessageParsingEntries = new DynamicColumnEntriesViewModel(() =>
                {
                    List<string> parsedEntriesCombined = new();
                    foreach (var messageParsing in messageParsings)
                    {
                        var parsedEntries = _messageParsingHandler.ParseMessage(messageParsing, logItem.Record);
                        parsedEntriesCombined.AddRange(parsedEntries);
                    }
                    return parsedEntriesCombined;
                });
            }

            messageParsingColumnsToSet = new DynamicColumnsViewModel(extractedColumns.Select(x => x.extractedColumn).ToArray());
        }

        _uiDispatcher.BeginInvoke(() =>
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

    public ColorizeByRecord ColorizeBy
    {
        get => GetOrDefault<ColorizeByRecord>();
        set => RaiseAndSetIfChanged(value);
    }

    public ObservableCollection<ColorizeByRecord> ColorizeByOptions { get; } = new();

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
