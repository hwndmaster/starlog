using Genius.Atom.UI.Forms;

namespace Genius.Starlog.UI;

public interface IHasDeleteCommand
{
    IActionCommand DeleteCommand { get; }
}
