using System.Windows.Data;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.ViewModels;

public sealed class LogFilterViewModel : ViewModelBase, ILogFilterCategoryViewModel
{
    private readonly ProfileFilterBase _profileFilter;

    public LogFilterViewModel(ProfileFilterBase profileFilter)
    {
        _profileFilter = profileFilter.NotNull();
    }

    public string Title => _profileFilter.LogFilter.Name;
    public string Icon => "FolderFavs32";
    public CollectionViewSource CategoryItemsView { get; } = new();

    public bool IsSelected
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }
}
