using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.ProfileFilters;

public sealed class TimeAgoProfileFilterSettingsViewModel : ProfileFilterSettingsViewModel<TimeAgoProfileFilter>
{
    public TimeAgoProfileFilterSettingsViewModel(TimeAgoProfileFilter profileFilter)
        : base(profileFilter)
    {
        // Members initialization:
        ResetChangesInternal();

        // Subscriptions:
        this.WhenAnyChanged(x => x.MinAgo, x => x.SecAgo)
            .Subscribe(_ => Name = "Time recent " + TimeSpan.FromSeconds(MinAgo * 60 + SecAgo).ToDisplayString());
    }

    protected override void CommitChangesInternal()
    {
        _profileFilter.TimeAgo = TimeSpan.FromSeconds(MinAgo * 60 + SecAgo);
    }

    protected override void ResetChangesInternal()
    {
        MinAgo = (int)_profileFilter.TimeAgo.TotalMinutes;
        SecAgo = _profileFilter.TimeAgo.Seconds;
    }

    public int MinAgo
    {
        get => GetOrDefault<int>();
        set => RaiseAndSetIfChanged(value);
    }

    public int SecAgo
    {
        get => GetOrDefault<int>();
        set => RaiseAndSetIfChanged(value);
    }
}
