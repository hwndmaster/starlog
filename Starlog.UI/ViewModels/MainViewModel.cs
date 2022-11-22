using System.Collections.Immutable;
using Genius.Atom.UI.Forms;

namespace Genius.Starlog.UI.ViewModels;

public interface IMainViewModel : IViewModel
{
    ImmutableArray<ITabViewModel> Tabs { get; }
    int SelectedTabIndex { get; set; }
}

internal sealed class MainViewModel : ViewModelBase, IMainViewModel
{
    public MainViewModel(
        IProfilesViewModel profiles,
        ILogsViewModel logs,
        ISettingsViewModel settings)
    {
        Tabs = new ITabViewModel[] {
            profiles,
            logs,
            settings
        }.ToImmutableArray();
    }

    public ImmutableArray<ITabViewModel> Tabs { get; }

    public int SelectedTabIndex
    {
        get => GetOrDefault<int>();
        set => RaiseAndSetIfChanged(value, (@old, @new) => {
            Tabs[@old].Deactivated.Execute(null);
            Tabs[@new].Activated.Execute(null);
        });
    }
}
