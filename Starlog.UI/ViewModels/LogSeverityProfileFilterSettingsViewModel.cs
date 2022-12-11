using System.Collections.ObjectModel;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.ViewModels;

public sealed class LogSeverityProfileFilterSettingsViewModel : ProfileFilterSettingsViewModel
{
    public LogSeverityProfileFilterSettingsViewModel(LogSeveritiesProfileFilter profileFilter)
        : base(profileFilter)
    {
        LogSeverities = Enum.GetValues<LogSeverity>();
        SelectedLogSeverities = new ObservableCollection<LogSeverity>(profileFilter.LogSeverities);
        SelectedLogSeverities.WhenCollectionChanged().Subscribe(_ =>
        {
            profileFilter.LogSeverities = SelectedLogSeverities.ToArray();
            Name = LimitNameLength("Severities: " + string.Join(", ", profileFilter.LogSeverities));
        });
    }

    public LogSeverity[] LogSeverities { get; }
    public ObservableCollection<LogSeverity> SelectedLogSeverities { get; }
}
