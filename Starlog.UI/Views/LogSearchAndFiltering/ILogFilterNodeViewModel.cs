using System.Windows.Data;

namespace Genius.Starlog.UI.Views.LogSearchAndFiltering;

public interface ILogFilterNodeViewModel : IHasPinnedFlag, IHasCanPinFlag
{
    string Title { get; }
    string Icon { get; }
    bool CanAddChildren { get; }
    bool CanModifyOrDelete { get; }
    bool IsExpanded { get; }
    CollectionViewSource CategoryItemsView { get; }
    IActionCommand AddChildCommand { get; }
    IActionCommand ModifyCommand { get; }
    IActionCommand DeleteCommand { get; }
}
