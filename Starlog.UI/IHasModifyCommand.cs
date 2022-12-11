using Genius.Atom.UI.Forms;

namespace Genius.Starlog.UI;

public interface IHasModifyCommand
{
    IActionCommand ModifyCommand { get; }
}
