using System.Collections.ObjectModel;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.ViewModels;

public sealed class LogLevelsProfileFilterSettingsViewModel : ProfileFilterSettingsViewModel
{
    public LogLevelsProfileFilterSettingsViewModel(LogLevelsProfileFilter profileFilter,
        ILogContainer logContainer)
        : base(profileFilter)
    {
        Guard.NotNull(logContainer);

        LogLevels = logContainer.GetLogLevels().Select(x => x.Name)
            .Union(profileFilter.LogLevels)
            .ToArray();
        SelectedLogLevels = new ObservableCollection<string>(profileFilter.LogLevels);
        SelectedLogLevels.WhenCollectionChanged().Subscribe(_ =>
        {
            profileFilter.LogLevels = SelectedLogLevels.ToArray();
            Name = profileFilter.LogLevels.Any()
                ? LimitNameLength("Levels: " + string.Join(", ", profileFilter.LogLevels))
                : profileFilter.LogFilter.Name;
        });
    }

    public ICollection<string> LogLevels { get; }
    public ObservableCollection<string> SelectedLogLevels { get; }
}
