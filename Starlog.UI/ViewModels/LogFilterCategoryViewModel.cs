using System.Collections.ObjectModel;
using System.Windows.Data;
using Genius.Atom.UI.Forms;

namespace Genius.Starlog.UI.ViewModels;

public interface ILogFilterCategoryViewModel
{
    string Title { get; }
    string Icon { get; }
    bool IsExpanded { get; }
    CollectionViewSource CategoryItemsView { get; }
}

public sealed class LogFilterCategoryViewModel<TChildViewModel> : ViewModelBase, ILogFilterCategoryViewModel
    where TChildViewModel : ILogFilterCategoryViewModel
{
    public LogFilterCategoryViewModel(string title, string icon, bool sort = false, bool expanded = false)
    {
        Title = title.NotNull();
        Icon = icon.NotNull();
        IsExpanded = expanded;

        CategoryItemsView.Source = CategoryItems;
        if (sort)
        {
            CategoryItemsView.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(ILogFilterCategoryViewModel.Title), System.ComponentModel.ListSortDirection.Ascending));
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

    public string Title { get; }
    public string Icon { get; }

    public bool IsExpanded
    {
        get => GetOrDefault<bool>();
        set => RaiseAndSetIfChanged(value);
    }

    public ObservableCollection<TChildViewModel> CategoryItems { get; } = new();
    public CollectionViewSource CategoryItemsView { get; } = new CollectionViewSource();
}
