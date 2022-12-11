using System.Windows.Data;
using Genius.Atom.UI.Forms;

namespace Genius.Starlog.UI.ViewModels;

public interface ILogFilterNodeViewModel
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
