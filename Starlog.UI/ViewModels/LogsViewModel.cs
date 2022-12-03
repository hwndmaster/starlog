using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Genius.Atom.UI.Forms;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.UI.AutoGridBuilders;
using ReactiveUI;

namespace Genius.Starlog.UI.ViewModels;

public interface ILogsViewModel : ITabViewModel
{
    void LoadCurrentProfile();
}

public sealed class LogsViewModel : TabViewModelBase, ILogsViewModel
{
    private readonly ILogContainer _logContainer;
    private IDisposable? _logsAddedSubscription;

    public LogsViewModel(ILogContainer logContainer,
        LogItemAutoGridBuilder autoGridBuilder)
    {
        _logContainer = logContainer.NotNull();
        AutoGridBuilder = autoGridBuilder.NotNull();

        ShareCommand = new ActionCommand(_ => throw new NotImplementedException());

        this.WhenChanged(x => x.ColorizeBy).Subscribe(_ =>
        {
            var colorizeByThread = ColorizeBy.Equals("T", StringComparison.Ordinal);
            foreach (var logItem in LogItems)
            {
                logItem.ColorizeByThread = colorizeByThread;
            }
        });
    }

    public void LoadCurrentProfile()
    {
        _logsAddedSubscription?.Dispose();
        LogItems.Clear();

        var logs = _logContainer.GetLogs();
        _logsAddedSubscription = _logContainer.LogsAdded
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => AddLogs(x));
        AddLogs(logs);
    }

    private void AddLogs(ImmutableArray<LogRecord> logs)
    {
        foreach (var log in logs.OrderBy(x => x.DateTime))
        {
            LogItems.Add(new LogItemViewModel(log));
        }
    }

    [FilterContext]
    public string Filter
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public string ColorizeBy
    {
        get => GetOrDefault("L");
        set => RaiseAndSetIfChanged(value);
    }

    public IAutoGridBuilder AutoGridBuilder { get; }

    public ObservableCollection<ILogItemViewModel> LogItems { get; }
        = new TypedObservableList<ILogItemViewModel, LogItemViewModel>();

    public IActionCommand ShareCommand { get; }
}
