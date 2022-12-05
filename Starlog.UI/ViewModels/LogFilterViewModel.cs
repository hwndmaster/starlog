using System.Windows.Data;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.ViewModels;

public sealed class LogFilterViewModel : ViewModelBase, ILogFilterCategoryViewModel
{
    public LogFilterViewModel(ProfileFilterBase profileFilter)
    {
        Filter = profileFilter.NotNull();
    }

    public ProfileFilterBase Filter { get; }
    public string Title => Filter.LogFilter.Name;
    public string Icon => "FolderFavs32";
    public CollectionViewSource CategoryItemsView { get; } = new();
}
