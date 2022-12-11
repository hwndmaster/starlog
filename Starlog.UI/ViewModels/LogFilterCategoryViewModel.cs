using System.Collections.ObjectModel;
using System.Windows.Data;
using Genius.Atom.UI.Forms;

namespace Genius.Starlog.UI.ViewModels;

public sealed class LogFilterCategoryViewModel<TChildViewModel> : ViewModelBase, ILogFilterNodeViewModel
    where TChildViewModel : ILogFilterNodeViewModel
{
    public LogFilterCategoryViewModel(string title, string icon, bool sort = false, bool expanded = false, bool canAddChildren = false)
    {
        Title = title.NotNull();
        Icon = icon.NotNull();
        CanAddChildren = canAddChildren;
        IsExpanded = expanded;

        AddChildCommand = new ActionCommand();

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

    internal void RemoveItem(TChildViewModel item)
    {
        CategoryItems.Remove(item);
        CategoryItemsView.View.Refresh();
    }

    public string Title { get; }
    public string Icon { get; }

    public bool CanAddChildren { get; }
    public bool CanModifyOrDelete => false;

    public bool IsExpanded
    {
        get => GetOrDefault<bool>();
        set => RaiseAndSetIfChanged(value);
    }

    public ObservableCollection<TChildViewModel> CategoryItems { get; } = new();
    public CollectionViewSource CategoryItemsView { get; } = new CollectionViewSource();

    public IActionCommand AddChildCommand { get; }
    public IActionCommand ModifyCommand => throw new NotSupportedException();
    public IActionCommand DeleteCommand => throw new NotSupportedException();
}
