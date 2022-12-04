using System.Collections.ObjectModel;
using System.Windows.Data;
using Genius.Atom.UI.Forms;

namespace Genius.Starlog.UI.ViewModels;

public interface ILogFilterCategoryViewModel : ISelectable
{
    string Title { get; }
    string Icon { get; }
    CollectionViewSource CategoryItemsView { get; }
}

public sealed class LogFilterCategoryViewModel<TChildViewModel> : ViewModelBase, ILogFilterCategoryViewModel
    where TChildViewModel : ILogFilterCategoryViewModel
{
    public LogFilterCategoryViewModel(string title, string icon, bool sort = false)
    {
        Title = title.NotNull();
        Icon = icon.NotNull();

        CategoryItemsView.Source = CategoryItems;
        if (sort)
        {
            CategoryItemsView.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(ILogFilterCategoryViewModel.Title), System.ComponentModel.ListSortDirection.Ascending));
        }
    }

    public string Title { get; }
    public string Icon { get; }

    public ObservableCollection<TChildViewModel> CategoryItems { get; } = new();
    public CollectionViewSource CategoryItemsView { get; } = new CollectionViewSource();

    public bool IsSelected
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }
}
