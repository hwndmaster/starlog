namespace Genius.Starlog.UI.Views.LogSearchAndFiltering;

public sealed class LogFilterBookmarkedCategoryViewModel : LogFilterCategoryViewModel<LogFilterViewModel>
{
    public LogFilterBookmarkedCategoryViewModel()
        : base("Bookmarked", "FolderFavs32", false, false, false)
    {
    }
}
