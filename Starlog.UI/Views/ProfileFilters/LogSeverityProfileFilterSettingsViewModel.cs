using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.ProfileFilters;

public sealed class LogSeverityProfileFilterSettingsViewModel : ProfileFilterSettingsViewModel<LogSeveritiesProfileFilter>
{
    public LogSeverityProfileFilterSettingsViewModel(LogSeveritiesProfileFilter profileFilter)
        : base(profileFilter)
    {
        // Members initialization:
        LogSeverities = Enum.GetValues<LogSeverity>();
        SelectedLogSeverities = new ObservableCollection<LogSeverity>(profileFilter.LogSeverities);

        // Subscriptions:
        SelectedLogSeverities.WhenCollectionChanged()
            .Select(_ => Unit.Default)
            .Merge(this.WhenChanged(x => x.Exclude).Select(_ => Unit.Default))
            .Subscribe(_ =>
        {
            Name = SelectedLogSeverities.Any()
                ? LimitNameLength((Exclude ? "Not " : string.Empty) + "Severities: " + string.Join(", ", SelectedLogSeverities))
                : profileFilter.LogFilter.Name;
        });
    }

    protected override void CommitChangesInternal()
    {
        _profileFilter.LogSeverities = SelectedLogSeverities.ToArray();
        _profileFilter.Exclude = Exclude;
    }

    protected override void ResetChangesInternal()
    {
        SelectedLogSeverities.Clear();
        foreach (var severity in _profileFilter.LogSeverities)
        {
            SelectedLogSeverities.Add(severity);
        }

        Exclude = _profileFilter.Exclude;
    }

    public LogSeverity[] LogSeverities { get; }
    public ObservableCollection<LogSeverity> SelectedLogSeverities { get; }

    public bool Exclude
    {
        get => GetOrDefault(_profileFilter.Exclude);
        set => RaiseAndSetIfChanged(value);
    }
}
