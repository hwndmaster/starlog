using System.Collections.ObjectModel;
using System.Windows.Data;

namespace Genius.Starlog.UI.Views.LogSearchAndFiltering;

public class LogFilterCategoryViewModel<TChildViewModel> : ViewModelBase, ILogFilterNodeViewModel
    where TChildViewModel : ILogFilterNodeViewModel
{
    public LogFilterCategoryViewModel(string title, string icon, bool sort = false, bool expanded = false, bool canAddChildren = false)
    {
        Title = title.NotNull();
        Icon = icon.NotNull();
        CanAddChildren = canAddChildren;
        IsExpanded = expanded;

        CategoryItemsView.Source = CategoryItems;
        if (sort)
        {
            CategoryItemsView.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(ILogFilterNodeViewModel.Title), System.ComponentModel.ListSortDirection.Ascending));
        }
    }

    internal void AddItems(IEnumerable<TChildViewModel> items)
    {
        foreach (var item in items)
        {
            CategoryItems.Add(item);
        }
        CategoryItemsView.View.Refresh();
    }

    internal void Remove(IEnumerable<TChildViewModel> items)
    {
        foreach (var item in items)
        {
            CategoryItems.Remove(item);
        }
        CategoryItemsView.View.Refresh();
    }

    internal void RemoveAll()
    {
        while (CategoryItems.Count > 0)
        {
            CategoryItems.RemoveAt(0);
        }
        CategoryItemsView.View.Refresh();
    }

    internal void RemoveItem(TChildViewModel item)
    {
        CategoryItems.Remove(item);
        CategoryItemsView.View.Refresh();
    }

    public string Title { get; }
    public string Icon { get; }

    public bool CanAddChildren { get; }
    public bool CanModifyOrDelete => false;
    public bool CanPin => false;

    public bool IsExpanded
    {
        get => GetOrDefault<bool>();
        set => RaiseAndSetIfChanged(value);
    }

    public bool IsPinned { get; set; }

    public ObservableCollection<TChildViewModel> CategoryItems { get; } = new();
    public CollectionViewSource CategoryItemsView { get; } = new CollectionViewSource();

    public IActionCommand AddChildCommand { get; } = new ActionCommand();
    public IActionCommand ModifyCommand => throw new NotSupportedException();
    public IActionCommand DeleteCommand => throw new NotSupportedException();
    public IActionCommand PinCommand { get; } = new ActionCommand(_ => throw new NotSupportedException());
}
