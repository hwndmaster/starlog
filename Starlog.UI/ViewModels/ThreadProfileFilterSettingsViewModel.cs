using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Genius.Atom.UI.Forms;
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

        Threads = logContainer.GetThreads();
        SelectedThreads = new ObservableCollection<string>(profileFilter.Threads);
        SelectedThreads.WhenCollectionChanged().Subscribe(_ =>
        {
            profileFilter.Threads = SelectedThreads.ToArray();
            Name = LimitNameLength("Threads: " + string.Join(", ", profileFilter.Threads));
        });
    }

    public ImmutableArray<string> Threads { get; }
    public ObservableCollection<string> SelectedThreads { get; }
}
