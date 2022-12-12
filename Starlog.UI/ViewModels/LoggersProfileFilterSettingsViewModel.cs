using System.Collections.ObjectModel;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.ViewModels;

public sealed class LoggersProfileFilterSettingsViewModel : ProfileFilterSettingsViewModel
{
    public LoggersProfileFilterSettingsViewModel(LoggersProfileFilter profileFilter,
        ILogContainer logContainer)
        : base(profileFilter)
    {
        Guard.NotNull(logContainer);

        Loggers = logContainer.GetLoggers().Select(x => x.Name)
            .Union(profileFilter.LoggerNames)
            .OrderBy(x => x)
            .ToArray();
        SelectedLoggers = new ObservableCollection<string>(profileFilter.LoggerNames);
        SelectedLoggers.WhenCollectionChanged().Subscribe(_ =>
        {
            profileFilter.LoggerNames = SelectedLoggers.ToArray();
            Name = profileFilter.LoggerNames.Any()
                ? LimitNameLength("Loggers: " + string.Join(", ", profileFilter.LoggerNames))
                : profileFilter.LogFilter.Name;
        });
    }

    public ICollection<string> Loggers { get; }
    public ObservableCollection<string> SelectedLoggers { get; }
}
