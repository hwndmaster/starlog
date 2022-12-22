using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.UI.Views.LogSearchAndFiltering;

/// <summary>
///   This view model covers:
///   - Message search text input in Logs.xaml
///   - Time range slider in Logs.xaml
/// </summary>
public interface ILogsSearchViewModel : IViewModel
{
    LogRecordSearchContext CreateContext();
    void Reconcile(int existingLogsCount, ICollection<LogRecord> logs);

    IObservable<Unit> SearchChanged { get; }
    string Text { get; set; }
    bool UseRegex { get; set; }
    double MinDateTimeTicks { get; set; }
    double MaxDateTimeTicks { get; set; }
    double SelectedDateTimeFromTicks { get; set; }
    double SelectedDateTimeToTicks { get; set; }
}

public sealed class LogsSearchViewModel : ViewModelBase, ILogsSearchViewModel
{
    static readonly long OneMinuteTicks = TimeSpan.FromMinutes(1).Ticks;
    static readonly long FiveSecondTicks = TimeSpan.FromSeconds(5).Ticks;

    private readonly ISubject<Unit> _searchChanged = new Subject<Unit>();

    public LogsSearchViewModel()
    {
        // Actions:
        SetTimeRangeTo1MinuteCommand = new ActionCommand(_ => SetTimeRange(OneMinuteTicks));
        SetTimeRangeTo5SecondCommand = new ActionCommand(_ => SetTimeRange(FiveSecondTicks));

        ResetTimeRangeCommand = new ActionCommand(_ =>
        {
            SelectedDateTimeFromTicks = MinDateTimeTicks;
            SelectedDateTimeToTicks = MaxDateTimeTicks;
        });

        UseRegexSwitchCommand = new ActionCommand(_ =>
        {
            UseRegex = !UseRegex;
            if (!string.IsNullOrWhiteSpace(Text))
            {
                _searchChanged.OnNext(Unit.Default);
            }
        });

        // Subscriptions:
        this.WhenAnyChanged(x => x.Text, x => x.SelectedDateTimeFromTicks, x => x.SelectedDateTimeToTicks)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(_ => _searchChanged.OnNext(Unit.Default));
    }

    public LogRecordSearchContext CreateContext()
    {
        var messageSearchIncluded = !string.IsNullOrWhiteSpace(Text);

        Regex? filterRegex = null;
        if (UseRegex)
        {
            try
            {
                filterRegex = new Regex(Text, RegexOptions.IgnoreCase);
            }
            catch (Exception)
            {
                filterRegex = null;
            }
        }

        DateTimeOffset? dateFrom = null;
        DateTimeOffset? dateTo = null;
        if (MinDateTimeTicks != SelectedDateTimeFromTicks
            || MaxDateTimeTicks != SelectedDateTimeToTicks)
        {
            dateFrom = new DateTimeOffset((long)SelectedDateTimeFromTicks, TimeSpan.Zero);
            dateTo = new DateTimeOffset((long)SelectedDateTimeToTicks, TimeSpan.Zero);
        }

        return new(messageSearchIncluded, Text, filterRegex, dateFrom, dateTo);
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

    private void SetTimeRange(long rangeTicks)
    {
        SelectedDateTimeFromTicks = Math.Max(SelectedDateTimeToTicks - rangeTicks, MinDateTimeTicks);
        if ((SelectedDateTimeToTicks - SelectedDateTimeFromTicks) < rangeTicks)
        {
            SelectedDateTimeToTicks = Math.Min(MaxDateTimeTicks, SelectedDateTimeFromTicks + rangeTicks);
        }
    }

    public IObservable<Unit> SearchChanged => _searchChanged;

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

    public IActionCommand UseRegexSwitchCommand { get; }
    public IActionCommand SetTimeRangeTo1MinuteCommand { get; }
    public IActionCommand SetTimeRangeTo5SecondCommand { get; }
    public IActionCommand ResetTimeRangeCommand { get; }
}
