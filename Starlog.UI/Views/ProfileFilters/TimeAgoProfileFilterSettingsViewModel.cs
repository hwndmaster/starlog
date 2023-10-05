using System.Reactive.Linq;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Helpers;

namespace Genius.Starlog.UI.Views.ProfileFilters;

// TODO: Cover with unit tests
public sealed class TimeAgoProfileFilterSettingsViewModel : ProfileFilterSettingsViewModel<TimeAgoProfileFilter>
{
    public TimeAgoProfileFilterSettingsViewModel(TimeAgoProfileFilter profileFilter, ILogContainer logContainer)
        : base(profileFilter)
    {
        Guard.NotNull(logContainer);

        // Members initialization:
        ResetChangesInternal();

        // Subscriptions:
        this.WhenAnyChanged(x => x.MinAgo, x => x.SecAgo)
            .Subscribe(_ =>
                Name = "Time recent " + TimeSpan.FromSeconds(MinAgo * 60 + SecAgo).ToDisplayString());
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
