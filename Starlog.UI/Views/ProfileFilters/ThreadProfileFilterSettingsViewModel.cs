using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.ProfileFilters;

public sealed class ThreadProfileFilterSettingsViewModel : ProfileFilterSettingsViewModel<ThreadsProfileFilter>
{
    public ThreadProfileFilterSettingsViewModel(ThreadsProfileFilter profileFilter, ILogContainer logContainer)
        : base(profileFilter)
    {
        Guard.NotNull(logContainer);

        // Members initialization:
        Threads = logContainer.GetThreads()
            .Union(profileFilter.Threads)
            .Order()
            .ToImmutableArray();
        SelectedThreads = new ObservableCollection<string>(profileFilter.Threads);

        // Subscriptions:
        SelectedThreads.WhenCollectionChanged()
            .Select(_ => Unit.Default)
            .Merge(this.WhenChanged(x => x.Exclude).Select(_ => Unit.Default))
            .Subscribe(_ =>
            {
                Name = SelectedThreads.Any()
                    ? LimitNameLength((Exclude ? "Not " : string.Empty) + "Threads: " + string.Join(", ", SelectedThreads))
                    : profileFilter.LogFilter.Name;
            });
    }

    protected override void CommitChangesInternal()
    {
        _profileFilter.Threads = SelectedThreads.ToArray();
        _profileFilter.Exclude = Exclude;
    }

    protected override void ResetChangesInternal()
    {
        SelectedThreads.Clear();
        foreach (var thread in _profileFilter.Threads)
        {
            SelectedThreads.Add(thread);
        }

        Exclude = _profileFilter.Exclude;
    }

    public ImmutableArray<string> Threads { get; }
    public ObservableCollection<string> SelectedThreads { get; }

    public bool Exclude
    {
        get => GetOrDefault(_profileFilter.Exclude);
        set => RaiseAndSetIfChanged(value);
    }
}
