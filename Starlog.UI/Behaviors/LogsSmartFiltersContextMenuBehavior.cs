using System.Windows.Controls;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class LogsSmartFiltersContextMenuBehavior : Behavior<DataGrid>
{
    private sealed record FieldMenuItem(MenuItem MenuItem, int FieldId, string FieldName);

    private readonly ILogFieldsContainerReadonly _fieldsContainer;
    private readonly MenuItem _menuItemCreateFilter;
    private readonly FieldMenuItem[] _menuItemsFields;
    private readonly MenuItem _menuItemLevels;
    private readonly MenuItem _menuItemFiles;
    private readonly MenuItem _menuItemContainsMsg;

    public LogsSmartFiltersContextMenuBehavior()
    {
        var logContainer = App.ServiceProvider.GetRequiredService<ILogContainer>();
        var logFilterContainer = App.ServiceProvider.GetRequiredService<ILogFilterContainer>();
        _fieldsContainer = logContainer.GetFields();

        var menuItemTimeRange = new MenuItem { Header = "Time: In the selected range", Command = new ActionCommand(_ =>
        {
            var vm = (ILogsViewModel)AssociatedObject.DataContext;

            var minItem = vm.SelectedLogItems.MinBy(x => x.Record.DateTime).NotNull();
            var maxItem = vm.SelectedLogItems.MaxBy(x => x.Record.DateTime).NotNull();

            var name = LogFilterHelpers.ProposeNameForTimeRange(minItem.Record.DateTime, maxItem.Record.DateTime);
            var filter = logFilterContainer.CreateProfileFilter<TimeRangeProfileFilter>(name);
            filter.SetTimeFromToExtended(minItem.Record.DateTime, maxItem.Record.DateTime);

            vm.Filtering.ShowFlyoutForAddingNewFilter(filter);
        }) };

        var fieldsContainer = logContainer.GetFields();
        var fields = fieldsContainer.GetFields();
        _menuItemsFields = new FieldMenuItem[fields.Length];

        for (var i = 0; i < _menuItemsFields.Length; i++)
        {
            var fieldId = i;
            var name = fields[i].FieldName;
            name = char.ToUpperInvariant(name[0]) + name[1..];

            _menuItemsFields[i] = new FieldMenuItem(
                new MenuItem { Header = "{field_name}: {..., ...}", Command = new ActionCommand(_ =>
                {
                    var vm = (ILogsViewModel)AssociatedObject.DataContext;
                    var fieldValueIds = vm.SelectedLogItems.Select(x => x.Record.FieldValueIndices[fieldId]).Distinct().ToArray();
                    string[] fieldValues = fieldValueIds.Select(fieldValueId => _fieldsContainer.GetFieldValue(fieldId, fieldValueId)).ToArray();

                    name = LogFilterHelpers.ProposeNameForStringList(name, fieldValues, false);
                    var filter = logFilterContainer.CreateProfileFilter<FieldProfileFilter>(name);
                    filter.FieldId = fieldId;
                    filter.Exclude = false;
                    filter.Values = fieldValues;

                    vm.Filtering.ShowFlyoutForAddingNewFilter(filter);
                }) },
                fieldId,
                name);
        }
        _menuItemLevels = new MenuItem { Header = "Level(s): {..., ...}", Command = new ActionCommand(_ =>
        {
            var vm = (ILogsViewModel)AssociatedObject.DataContext;
            var levels = vm.SelectedLogItems.Select(x => x.Record.Level.Name).Distinct().ToArray();

            var name = LogFilterHelpers.ProposeNameForStringList("Levels", levels, false);
            var filter = logFilterContainer.CreateProfileFilter<LogLevelsProfileFilter>(name);
            filter.Exclude = false;
            filter.LogLevels = levels;

            vm.Filtering.ShowFlyoutForAddingNewFilter(filter);
        }) };
        _menuItemFiles = new MenuItem { Header = "Files(s): {..., ...}", Command = new ActionCommand(_ =>
        {
            var vm = (ILogsViewModel)AssociatedObject.DataContext;
            var files = vm.SelectedLogItems.Select(x => x.Record.Source.DisplayName).Distinct().ToArray();

            var name = LogFilterHelpers.ProposeNameForStringList("Files", files, false);
            var filter = logFilterContainer.CreateProfileFilter<FilesProfileFilter>(name);
            filter.Exclude = false;
            filter.FileNames = files;

            vm.Filtering.ShowFlyoutForAddingNewFilter(filter);
        }) };

        _menuItemContainsMsg = new MenuItem { Header = "Contains: {msg...}", Command = new ActionCommand(_ =>
        {
            var vm = (ILogsViewModel)AssociatedObject.DataContext;
            var msg = vm.SelectedLogItems[0].Record.Message;

            var name = LogFilterHelpers.LimitNameLength("Contains: '" + msg + "'");
            var filter = logFilterContainer.CreateProfileFilter<MessageProfileFilter>(name);
            filter.IsRegex = false;
            filter.MatchCasing = false;
            filter.IncludeArtifacts = false;
            filter.Pattern = msg;

            vm.Filtering.ShowFlyoutForAddingNewFilter(filter);
        }) };
        _menuItemCreateFilter = new MenuItem {
            Header = "Create filter",
            Items = {
                menuItemTimeRange,
                _menuItemLevels,
                _menuItemFiles,
                _menuItemContainsMsg
            }
        };
        foreach (var menuItem in _menuItemsFields)
        {
            _menuItemCreateFilter.Items.Add(menuItem);
        }
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

        foreach (var fieldMenuItem in _menuItemsFields)
        {
            var fieldValueIds = vm.SelectedLogItems.Select(x => x.Record.FieldValueIndices[fieldMenuItem.FieldId]).Distinct();
            string fieldValues = string.Join(", ", fieldValueIds.Select(fieldValueId => _fieldsContainer.GetFieldValue(fieldMenuItem.FieldId, fieldValueId)));

            fieldMenuItem.MenuItem.Header = LogFilterHelpers.LimitNameLength(fieldMenuItem.FieldName + "(s): " + fieldValues);
        }

        var levels = string.Join(", ", vm.SelectedLogItems.Select(x => x.Record.Level.Name).Distinct());
        _menuItemLevels.Header = LogFilterHelpers.LimitNameLength("Level(s): " + levels);

        var files = string.Join(", ", vm.SelectedLogItems.Select(x => x.Record.Source.DisplayName).Distinct());
        _menuItemFiles.Header = LogFilterHelpers.LimitNameLength("File(s): " + files);

        var msg = vm.SelectedLogItems[0].Record.Message;
        _menuItemContainsMsg.Header = LogFilterHelpers.LimitNameLength("Contains: " + msg);
    }
}
