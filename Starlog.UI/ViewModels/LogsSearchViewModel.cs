using System.Reactive.Linq;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.UI.ViewModels;

/// <summary>
///   This view model covers:
///   - Message search text input in Logs.xaml
///   - Time range slider in Logs.xaml
/// </summary>
public interface ILogsSearchViewModel : IViewModel
{
    string Text { get; set; }
    bool UseRegex { get; set; }
    double MinDateTimeTicks { get; set; }
    double MaxDateTimeTicks { get; set; }
    double SelectedDateTimeFromTicks { get; set; }
    double SelectedDateTimeToTicks { get; set; }

    void Reconcile(int existingLogsCount, ICollection<LogRecord> logs);
}

public sealed class LogsSearchViewModel : ViewModelBase, ILogsSearchViewModel
{
    static readonly long OneMinuteTicks = TimeSpan.FromMinutes(1).Ticks;
    static readonly long FiveSecondTicks = TimeSpan.FromSeconds(5).Ticks;

    public LogsSearchViewModel()
    {
        SetTimeRangeTo1MinuteCommand = new ActionCommand(_ => NewMethod(OneMinuteTicks));
        SetTimeRangeTo5SecondCommand = new ActionCommand(_ => NewMethod(FiveSecondTicks));

        ResetTimeRangeCommand = new ActionCommand(_ =>
        {
            SelectedDateTimeFromTicks = MinDateTimeTicks;
            SelectedDateTimeToTicks = MaxDateTimeTicks;
        });
    }

    private void NewMethod(long rangeTicks)
    {
        SelectedDateTimeToTicks = Math.Min(SelectedDateTimeFromTicks + rangeTicks, MaxDateTimeTicks);
        if ((SelectedDateTimeToTicks - SelectedDateTimeFromTicks) < rangeTicks)
        {
            SelectedDateTimeFromTicks = Math.Max(MinDateTimeTicks, SelectedDateTimeToTicks - rangeTicks);
        }
    }

    public void Reconcile(int existingLogsCount, ICollection<LogRecord> addedLogs)
    {
        var wasMaxTime = MaxDateTimeTicks == SelectedDateTimeToTicks;
        MinDateTimeTicks = addedLogs.Min(x => x.DateTime).UtcTicks;
        MaxDateTimeTicks = SelectedDateTimeToTicks = addedLogs.Max(x => x.DateTime).UtcTicks;

        if (existingLogsCount == 0)
        {
            SelectedDateTimeFromTicks = MinDateTimeTicks;
            SelectedDateTimeToTicks = MaxDateTimeTicks;
        }
        else if (wasMaxTime)
        {
            SelectedDateTimeToTicks = MaxDateTimeTicks;
        }
    }

    public string Text
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public bool UseRegex
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public double MinDateTimeTicks
    {
        get => GetOrDefault(0d);
        set => RaiseAndSetIfChanged(value);
    }

    public double MaxDateTimeTicks
    {
        get => GetOrDefault(0d);
        set => RaiseAndSetIfChanged(value);
    }

    public double SelectedDateTimeFromTicks
    {
        get => GetOrDefault(0d);
        set => RaiseAndSetIfChanged(value);
    }

    public double SelectedDateTimeToTicks
    {
        get => GetOrDefault(0d);
        set => RaiseAndSetIfChanged(value);
    }

    public IActionCommand SetTimeRangeTo1MinuteCommand { get; }
    public IActionCommand SetTimeRangeTo5SecondCommand { get; }
    public IActionCommand ResetTimeRangeCommand { get; }
}
