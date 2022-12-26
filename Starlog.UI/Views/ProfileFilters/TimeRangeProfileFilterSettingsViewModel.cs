using System.Reactive.Linq;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Validation;

namespace Genius.Starlog.UI.Views.ProfileFilters;

public sealed class TimeRangeProfileFilterSettingsViewModel : ProfileFilterSettingsViewModel<TimeRangeProfileFilter>
{
    public TimeRangeProfileFilterSettingsViewModel(TimeRangeProfileFilter profileFilter, ILogContainer logContainer, bool isNewFilter)
        : base(profileFilter)
    {
        Guard.NotNull(logContainer);

        // Members initialization:
        AddValidationRule(new [] { nameof(TimeFrom), nameof(TimeTo) },
            new ValueRangeValidationRule<DateTime>(() => TimeFrom, () => TimeTo));

        if (isNewFilter)
        {
            var currentTime = DateTimeOffset.Now.UtcDateTime + DateTimeOffset.Now.Offset;
            var todayTime = new DateTimeOffset(currentTime.Year, currentTime.Month, currentTime.Day, 0, 0, 0, TimeSpan.Zero);
            profileFilter.TimeFrom = todayTime;
            profileFilter.TimeTo = currentTime;
        }

        ResetChangesInternal();

        // Subscriptions:
        this.WhenAnyChanged(x => x.TimeFrom, x => x.TimeTo)
            .Subscribe(_ =>
                Name = "Time from " + TimeFrom.ToString() + " to " + (
                    TimeTo.Date == TimeFrom.Date ? TimeTo.ToLongTimeString() : TimeTo.ToString()
                ));
    }

    protected override void CommitChangesInternal()
    {
        _profileFilter.TimeFrom = new DateTimeOffset(TimeFrom, TimeSpan.Zero);
        _profileFilter.TimeTo = new DateTimeOffset(TimeTo, TimeSpan.Zero);
    }

    protected override void ResetChangesInternal()
    {
        TimeFrom = new DateTime(_profileFilter.TimeFrom.UtcTicks, DateTimeKind.Utc);
        TimeTo = new DateTime(_profileFilter.TimeTo.UtcTicks, DateTimeKind.Utc);
    }

    public DateTime TimeFrom
    {
        get => GetOrDefault<DateTime>();
        set => RaiseAndSetIfChanged(value);
    }

    public DateTime TimeTo
    {
        get => GetOrDefault<DateTime>();
        set => RaiseAndSetIfChanged(value);
    }
}
