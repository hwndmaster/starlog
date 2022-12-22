using System.Collections.ObjectModel;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.ProfileFilters;

public sealed class LogSeverityProfileFilterSettingsViewModel : ProfileFilterSettingsViewModel
{
    public LogSeverityProfileFilterSettingsViewModel(LogSeveritiesProfileFilter profileFilter)
        : base(profileFilter)
    {
        // Members initialization:
        LogSeverities = Enum.GetValues<LogSeverity>();
        SelectedLogSeverities = new ObservableCollection<LogSeverity>(profileFilter.LogSeverities);

        // Subscriptions:
        SelectedLogSeverities.WhenCollectionChanged().Subscribe(_ =>
        {
            profileFilter.LogSeverities = SelectedLogSeverities.ToArray();
            Name = profileFilter.LogSeverities.Any()
                ? LimitNameLength("Severities: " + string.Join(", ", profileFilter.LogSeverities))
                : profileFilter.LogFilter.Name;
        });
    }

    public LogSeverity[] LogSeverities { get; }
    public ObservableCollection<LogSeverity> SelectedLogSeverities { get; }
}
