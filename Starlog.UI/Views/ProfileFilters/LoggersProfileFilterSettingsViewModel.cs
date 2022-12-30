using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.ProfileFilters;

public sealed class LoggersProfileFilterSettingsViewModel : ProfileFilterSettingsViewModel<LoggersProfileFilter>
{
    public LoggersProfileFilterSettingsViewModel(LoggersProfileFilter profileFilter, ILogContainer logContainer)
        : base(profileFilter)
    {
        Guard.NotNull(logContainer);

        // Members initialization:
        Loggers = logContainer.GetLoggers().Select(x => x.Name)
            .Union(profileFilter.LoggerNames)
            .Order()
            .ToArray();
        SelectedLoggers = new ObservableCollection<string>(profileFilter.LoggerNames);

        // Subscriptions:
        SelectedLoggers.WhenCollectionChanged()
            .Select(_ => Unit.Default)
            .Merge(this.WhenChanged(x => x.Exclude).Select(_ => Unit.Default))
            .Subscribe(_ =>
        {
            Name = SelectedLoggers.Any()
                ? LimitNameLength((Exclude ? "Not " : string.Empty) + "Loggers: " + string.Join(", ", SelectedLoggers))
                : profileFilter.LogFilter.Name;
        });
    }

    protected override void ResetChangesInternal()
    {
        SelectedLoggers.Clear();
        foreach (var loggerName in _profileFilter.LoggerNames)
        {
            SelectedLoggers.Add(loggerName);
        }

        Exclude = _profileFilter.Exclude;
    }

    protected override void CommitChangesInternal()
    {
        _profileFilter.LoggerNames = SelectedLoggers.ToArray();
        _profileFilter.Exclude = Exclude;
    }

    public ICollection<string> Loggers { get; }
    public ObservableCollection<string> SelectedLoggers { get; }

    public bool Exclude
    {
        get => GetOrDefault(_profileFilter.Exclude);
        set => RaiseAndSetIfChanged(value);
    }
}
