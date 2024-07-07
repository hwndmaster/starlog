using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Helpers;

namespace Genius.Starlog.UI.Views.ProfileFilters;

public sealed class LogLevelsProfileFilterSettingsViewModel : ProfileFilterSettingsViewModel<LogLevelsProfileFilter>
{
    public LogLevelsProfileFilterSettingsViewModel(LogLevelsProfileFilter profileFilter, ILogContainer logContainer)
        : base(profileFilter)
    {
        Guard.NotNull(logContainer);

        // Members initialization:
        LogLevels = logContainer.GetLogLevels().Select(x => x.Name)
            .Union(profileFilter.LogLevels)
            .ToArray();
        SelectedLogLevels = new ObservableCollection<string>(profileFilter.LogLevels);

        // Subscriptions:
        SelectedLogLevels.WhenCollectionChanged()
            .Select(_ => Unit.Default)
            .Merge(this.WhenChanged(x => x.Exclude).Select(_ => Unit.Default))
            .Subscribe(_ =>
            {
                Name = SelectedLogLevels.Any()
                    ? LogFilterHelpers.ProposeNameForStringList("Levels", SelectedLogLevels, Exclude)
                    : profileFilter.LogFilter.Name;
            });
    }

    protected override void CommitChangesInternal()
    {
        _profileFilter.LogLevels = SelectedLogLevels.ToArray();
        _profileFilter.Exclude = Exclude;
    }

    protected override void ResetChangesInternal()
    {
        SelectedLogLevels.Clear();
        foreach (var logLevel in _profileFilter.LogLevels)
        {
            SelectedLogLevels.Add(logLevel);
        }

        Exclude = _profileFilter.Exclude;
    }

    public ICollection<string> LogLevels { get; }
    public ObservableCollection<string> SelectedLogLevels { get; }

    public bool Exclude
    {
        get => GetOrDefault(_profileFilter.Exclude);
        set => RaiseAndSetIfChanged(value);
    }
}
