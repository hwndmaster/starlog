using System.Collections.Immutable;
using Genius.Starlog.Core;
using Genius.Starlog.UI.Views.Comparison;

namespace Genius.Starlog.UI.Views;

public interface IMainViewModel : IViewModel
{
    ImmutableArray<ITabViewModel> Tabs { get; }
    int SelectedTabIndex { get; set; }
    bool IsBusy { get; set; }
    bool IsComparisonAvailable { get; set; }
}

// TODO: Cover with unit tests
internal sealed class MainViewModel : ViewModelBase, IMainViewModel
{
    public MainViewModel(
        IProfilesViewModel profiles,
        ILogsViewModel logs,
        IComparisonViewModel compare,
        ISettingsViewModel settings,
        ICurrentProfile currentProfile)
    {
        Guard.NotNull(profiles);
        Guard.NotNull(logs);
        Guard.NotNull(compare);
        Guard.NotNull(settings);
        Guard.NotNull(currentProfile);

        Tabs = new ITabViewModel[] {
            profiles,
            logs,
            compare,
            settings
        }.ToImmutableArray();

        currentProfile.ProfileClosed.Subscribe(_ =>
            CurrentProfileName = "N/A");

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

    public bool IsComparisonAvailable
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public bool ComparisonFeatureEnabled => App.ComparisonFeatureEnabled;
}
