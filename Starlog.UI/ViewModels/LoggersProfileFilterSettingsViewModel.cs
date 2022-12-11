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

        Loggers = logContainer.GetLoggers().Select(x => x.Name).ToArray();
        SelectedLoggers = new ObservableCollection<string>(profileFilter.LoggerNames);
        SelectedLoggers.WhenCollectionChanged().Subscribe(_ =>
        {
            profileFilter.LoggerNames = SelectedLoggers.ToArray();
            Name = LimitNameLength("Loggers: " + string.Join(", ", profileFilter.LoggerNames));
        });
    }

    public ICollection<string> Loggers { get; }
    public ObservableCollection<string> SelectedLoggers { get; }
}
