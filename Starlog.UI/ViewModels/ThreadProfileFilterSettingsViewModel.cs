using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.ViewModels;

public sealed class ThreadProfileFilterSettingsViewModel : ProfileFilterSettingsViewModel
{
    public ThreadProfileFilterSettingsViewModel(ThreadsProfileFilter profileFilter,
        ILogContainer logContainer)
        : base(profileFilter)
    {
        Guard.NotNull(logContainer);

        // Members initialization:
        Threads = logContainer.GetThreads()
            .Union(profileFilter.Threads)
            .ToImmutableArray();
        SelectedThreads = new ObservableCollection<string>(profileFilter.Threads);

        // Subscriptions:
        SelectedThreads.WhenCollectionChanged().Subscribe(_ =>
        {
            profileFilter.Threads = SelectedThreads.ToArray();
            Name = profileFilter.Threads.Any()
                ? LimitNameLength("Threads: " + string.Join(", ", profileFilter.Threads))
                : profileFilter.LogFilter.Name;
        });
    }

    public ImmutableArray<string> Threads { get; }
    public ObservableCollection<string> SelectedThreads { get; }
}
