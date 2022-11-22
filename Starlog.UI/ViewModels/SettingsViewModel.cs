using Genius.Atom.UI.Forms;

namespace Genius.Starlog.UI.ViewModels;

public interface ISettingsViewModel : ITabViewModel
{ }

internal sealed class SettingsViewModel : TabViewModelBase, ISettingsViewModel
{
}
