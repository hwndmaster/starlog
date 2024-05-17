using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using MahApps.Metro.Controls;

namespace Genius.Starlog.UI.Views.LogSearchAndFiltering;

/// <summary>
///   This view model covers:
///   - Message search text input in Logs.xaml
///   - Time range slider in Logs.xaml
/// </summary>
public interface ILogsSearchViewModel : IViewModel
{
    LogRecordSearchContext CreateContext();
    void DropAllSearches();
    void Reconcile(int existingLogsCount, ICollection<LogRecord> addedLogs);

    IObservable<Unit> SearchChanged { get; }
    string Text { get; set; }
    bool UseRegex { get; set; }
    double MinDateTimeTicks { get; set; }
    double MaxDateTimeTicks { get; set; }
    double SelectedDateTimeFromTicks { get; set; }
    double SelectedDateTimeToTicks { get; set; }
}

// TODO: Cover with unit tests
public sealed class LogsSearchViewModel : DisposableViewModelBase, ILogsSearchViewModel
{
    static readonly long OneMinuteTicks = TimeSpan.FromMinutes(1).Ticks;
    static readonly long FiveSecondTicks = TimeSpan.FromSeconds(5).Ticks;

    private readonly Subject<Unit> _searchChanged = new();

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
            .Subscribe(_ => _searchChanged.OnNext(Unit.Default))
            .DisposeWith(Disposer);
    }

    public LogRecordSearchContext CreateContext()
    {
        var messageSearchIncluded = !string.IsNullOrWhiteSpace(Text);

        Regex? filterRegex = null;
        if (UseRegex && messageSearchIncluded)
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
        if (!MinDateTimeTicks.Equals(SelectedDateTimeFromTicks)
            || !MaxDateTimeTicks.Equals(SelectedDateTimeToTicks))
        {
            dateFrom = new DateTimeOffset((long)SelectedDateTimeFromTicks, TimeSpan.Zero);
            dateTo = new DateTimeOffset((long)SelectedDateTimeToTicks, TimeSpan.Zero);
        }

        return new(HasAnythingSpecified: messageSearchIncluded || dateFrom is not null || dateTo is not null,
            messageSearchIncluded, Text, filterRegex, dateFrom, dateTo);
    }

    public void DropAllSearches()
    {
        Text = string.Empty;
        SelectedDateTimeFromTicks = MinDateTimeTicks;
        SelectedDateTimeToTicks = MaxDateTimeTicks;
    }

    public void Reconcile(int existingLogsCount, ICollection<LogRecord> addedLogs)
    {
        var wasMinTime = MinDateTimeTicks.Equals(SelectedDateTimeFromTicks);
        var wasMaxTime = MaxDateTimeTicks.Equals(SelectedDateTimeToTicks);
        var wasRange = SelectedDateTimeToTicks - SelectedDateTimeFromTicks;
        MinDateTimeTicks = Math.Min(MinDateTimeTicks.IsZero() ? long.MaxValue : MinDateTimeTicks, addedLogs.Min(x => x.DateTime).UtcTicks);
        MaxDateTimeTicks = Math.Max(MaxDateTimeTicks, addedLogs.Max(x => x.DateTime).UtcTicks);

        if (existingLogsCount == 0)
        {
            SelectedDateTimeFromTicks = MinDateTimeTicks;
            SelectedDateTimeToTicks = MaxDateTimeTicks;
        }
        else
        {
            if (wasMaxTime)
            {
                SelectedDateTimeToTicks = MaxDateTimeTicks;
            }
            if (wasMinTime)
            {
                SelectedDateTimeFromTicks = MinDateTimeTicks;
            }
            else if (wasMaxTime)
            {
                SelectedDateTimeFromTicks = Math.Max(MinDateTimeTicks, SelectedDateTimeToTicks - wasRange);
            }
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
