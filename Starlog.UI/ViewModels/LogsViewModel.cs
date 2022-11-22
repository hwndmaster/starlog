using System.Collections.ObjectModel;
using Genius.Atom.UI.Forms;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.UI.AutoGridBuilders;

namespace Genius.Starlog.UI.ViewModels;

public interface ILogsViewModel : ITabViewModel
{
    void LoadCurrentProfile();
}

public sealed class LogsViewModel : TabViewModelBase, ILogsViewModel
{
    private readonly ILogContainer _logContainer;

    public LogsViewModel(ILogContainer logContainer,
        LogItemAutoGridBuilder autoGridBuilder)
    {
        _logContainer = logContainer.NotNull();
        AutoGridBuilder = autoGridBuilder.NotNull();
    }

    public void LoadCurrentProfile()
    {
        LogItems.Clear();

        var logRecords = _logContainer.GetLogs();
        foreach (var logRecord in logRecords)
        {
            LogItems.Add(new LogItemViewModel(logRecord));
        }
    }

    public IAutoGridBuilder AutoGridBuilder { get; }

    public ObservableCollection<ILogItemViewModel> LogItems { get; }
        = new TypedObservableList<ILogItemViewModel, LogItemViewModel>();
}
