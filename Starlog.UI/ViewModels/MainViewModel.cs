using System.Collections.Immutable;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.UI.ViewModels;

public interface IMainViewModel : IViewModel
{
    ImmutableArray<ITabViewModel> Tabs { get; }
    int SelectedTabIndex { get; set; }
    bool IsBusy { get; set; }
}

internal sealed class MainViewModel : ViewModelBase, IMainViewModel
{
    public MainViewModel(
        IProfilesViewModel profiles,
        ILogsViewModel logs,
        ISettingsViewModel settings,
        ICurrentProfile currentProfile)
    {
        Guard.NotNull(profiles);
        Guard.NotNull(logs);
        Guard.NotNull(settings);
        Guard.NotNull(currentProfile);

        Tabs = new ITabViewModel[] {
            profiles,
            logs,
            settings
        }.ToImmutableArray();

        currentProfile.ProfileChanged.Subscribe(profile =>
        {
            CurrentProfileName = profile is null
                ? "N/A"
                : $"{profile.Name} ({profile.Path})";
        });
    }

    public ImmutableArray<ITabViewModel> Tabs { get; }

    public string CurrentProfileName
    {
        get => GetOrDefault("N/A");
        set => RaiseAndSetIfChanged(value);
    }

    public int SelectedTabIndex
    {
        get => GetOrDefault<int>();
        set => RaiseAndSetIfChanged(value, (@old, @new) => {
            Tabs[@old].Deactivated.Execute(null);
            Tabs[@new].Activated.Execute(null);
        });
    }

    public bool IsBusy
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }
}
