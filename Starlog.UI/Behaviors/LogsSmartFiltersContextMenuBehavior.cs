using System.Windows.Controls;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class LogsSmartFiltersContextMenuBehavior : Behavior<DataGrid>
{
    private readonly ILogFilterContainer _logFilterContainer;

    private readonly MenuItem _menuItemCreateFilter;
    private readonly MenuItem _menuItemTimeRange;
    private readonly MenuItem _menuItemThreads;
    private readonly MenuItem _menuItemLoggers;
    private readonly MenuItem _menuItemLevels;
    private readonly MenuItem _menuItemContainsMsg;

    public LogsSmartFiltersContextMenuBehavior()
    {
        _logFilterContainer = App.ServiceProvider.GetRequiredService<ILogFilterContainer>();
        _menuItemTimeRange = new MenuItem { Header = "Time: In the selected range", Command = new ActionCommand(_ =>
        {
            var vm = (ILogsViewModel)AssociatedObject.DataContext;

            var minItem = vm.SelectedLogItems.MinBy(x => x.Record.DateTime).NotNull();
            var maxItem = vm.SelectedLogItems.MaxBy(x => x.Record.DateTime).NotNull();

            var name = LogFilterHelpers.ProposeNameForTimeRange(minItem.Record.DateTime, maxItem.Record.DateTime);
            var filter = _logFilterContainer.CreateProfileFilter<TimeRangeProfileFilter>(name);
            filter.SetTimeFromToExtended(minItem.Record.DateTime, maxItem.Record.DateTime);

            vm.Filtering.ShowFlyoutForAddingNewFilter(filter);
        }) };
        _menuItemThreads = new MenuItem { Header = "Thread(s): {..., ...}", Command = new ActionCommand(_ =>
        {
            var vm = (ILogsViewModel)AssociatedObject.DataContext;
            var threads = vm.SelectedLogItems.Select(x => x.Record.Thread).Distinct().ToArray();

            var name = LogFilterHelpers.ProposeNameForStringList("Threads", threads, false);
            var filter = _logFilterContainer.CreateProfileFilter<ThreadsProfileFilter>(name);
            filter.Exclude = false;
            filter.Threads = threads;

            vm.Filtering.ShowFlyoutForAddingNewFilter(filter);
        }) };
        _menuItemLoggers = new MenuItem { Header = "Logger(s): {..., ...}", Command = new ActionCommand(_ =>
        {
            var vm = (ILogsViewModel)AssociatedObject.DataContext;
            var loggers = vm.SelectedLogItems.Select(x => x.Record.Logger.Name).Distinct().ToArray();

            var name = LogFilterHelpers.ProposeNameForStringList("Loggers", loggers, false);
            var filter = _logFilterContainer.CreateProfileFilter<LoggersProfileFilter>(name);
            filter.Exclude = false;
            filter.LoggerNames = loggers;

            vm.Filtering.ShowFlyoutForAddingNewFilter(filter);
        }) };
        _menuItemLevels = new MenuItem { Header = "Level(s): {..., ...}", Command = new ActionCommand(_ =>
        {
            var vm = (ILogsViewModel)AssociatedObject.DataContext;
            var levels = vm.SelectedLogItems.Select(x => x.Record.Level.Name).Distinct().ToArray();

            var name = LogFilterHelpers.ProposeNameForStringList("Levels", levels, false);
            var filter = _logFilterContainer.CreateProfileFilter<LogLevelsProfileFilter>(name);
            filter.Exclude = false;
            filter.LogLevels = levels;

            vm.Filtering.ShowFlyoutForAddingNewFilter(filter);
        }) };
        _menuItemContainsMsg = new MenuItem { Header = "Contains: {msg...}", Command = new ActionCommand(_ =>
        {
            var vm = (ILogsViewModel)AssociatedObject.DataContext;
            var msg = vm.SelectedLogItems[0].Record.Message;

            var name = LogFilterHelpers.LimitNameLength("Contains: '" + msg + "'");
            var filter = _logFilterContainer.CreateProfileFilter<MessageProfileFilter>(name);
            filter.IsRegex = false;
            filter.MatchCasing = false;
            filter.IncludeArtifacts = false;
            filter.Pattern = msg;

            vm.Filtering.ShowFlyoutForAddingNewFilter(filter);
        }) };
        _menuItemCreateFilter = new MenuItem {
            Header = "Create filter",
            Items = {
                _menuItemTimeRange,
                _menuItemThreads,
                _menuItemLoggers,
                _menuItemLevels,
                _menuItemContainsMsg
            }
        };
        _menuItemCreateFilter.SubmenuOpened += OnSubmenuOpened;
    }

    protected override void OnAttached()
    {
        var contextMenu = WpfHelpers.EnsureDataGridRowContextMenu(AssociatedObject);
        contextMenu.Items.Add(_menuItemCreateFilter);
        contextMenu.ContextMenuOpening += OnContextMenuOpening;

        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        var contextMenu = WpfHelpers.EnsureDataGridRowContextMenu(AssociatedObject);
        contextMenu.Items.Remove(_menuItemCreateFilter);
        contextMenu.ContextMenuOpening -= OnContextMenuOpening;

        base.OnDetaching();
    }

    private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        var vm = (ILogsViewModel)AssociatedObject.DataContext;

        _menuItemCreateFilter.Visibility = vm.SelectedLogItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnSubmenuOpened(object sender, RoutedEventArgs e)
    {
        var vm = (ILogsViewModel)AssociatedObject.DataContext;

        var threads = string.Join(", ", vm.SelectedLogItems.Select(x => x.Record.Thread).Distinct());
        _menuItemThreads.Header = LogFilterHelpers.LimitNameLength("Thread(s): " + threads);

        var loggers = string.Join(", ", vm.SelectedLogItems.Select(x => x.Record.Logger.Name).Distinct());
        _menuItemLoggers.Header = LogFilterHelpers.LimitNameLength("Logger(s): " + loggers);

        var levels = string.Join(", ", vm.SelectedLogItems.Select(x => x.Record.Level.Name).Distinct());
        _menuItemLevels.Header = LogFilterHelpers.LimitNameLength("Level(s): " + levels);

        var msg = vm.SelectedLogItems[0].Record.Message;
        _menuItemContainsMsg.Header = LogFilterHelpers.LimitNameLength("Contains: " + msg);
    }
}
