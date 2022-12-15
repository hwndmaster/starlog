using System.Windows.Data;

namespace Genius.Starlog.UI.ViewModels;

public interface ILogFilterNodeViewModel : IHasPinnedFlag
{
    string Title { get; }
    string Icon { get; }
    bool CanAddChildren { get; }
    bool CanModifyOrDelete { get; }
    bool CanPin { get; }
    bool IsExpanded { get; }
    CollectionViewSource CategoryItemsView { get; }
    IActionCommand AddChildCommand { get; }
    IActionCommand ModifyCommand { get; }
    IActionCommand DeleteCommand { get; }
}
