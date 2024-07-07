using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Helpers;

namespace Genius.Starlog.UI.Views.ProfileFilters;

public sealed class FilesProfileFilterSettingsViewModel : ProfileFilterSettingsViewModel<FilesProfileFilter>
{
    public FilesProfileFilterSettingsViewModel(FilesProfileFilter profileFilter, ILogContainer logContainer)
        : base(profileFilter)
    {
        Guard.NotNull(logContainer);

        // Members initialization:
        Files = logContainer.GetSources().Select(x => x.DisplayName)
            .Union(profileFilter.FileNames)
            .Order()
            .ToArray();
        SelectedFiles = new ObservableCollection<string>(profileFilter.FileNames);

        // Subscriptions:
        SelectedFiles.WhenCollectionChanged()
            .Select(_ => Unit.Default)
            .Merge(this.WhenChanged(x => x.Exclude).Select(_ => Unit.Default))
            .Subscribe(_ =>
            {
                Name = SelectedFiles.Any()
                    ? LogFilterHelpers.ProposeNameForStringList("Files", SelectedFiles, Exclude)
                    : profileFilter.LogFilter.Name;
            });
    }

    protected override void CommitChangesInternal()
    {
        _profileFilter.FileNames = SelectedFiles.ToArray();
        _profileFilter.Exclude = Exclude;
    }

    protected override void ResetChangesInternal()
    {
        SelectedFiles.Clear();
        foreach (var logLevel in _profileFilter.FileNames)
        {
            SelectedFiles.Add(logLevel);
        }

        Exclude = _profileFilter.Exclude;
    }

    public ICollection<string> Files { get; }
    public ObservableCollection<string> SelectedFiles { get; }

    public bool Exclude
    {
        get => GetOrDefault(_profileFilter.Exclude);
        set => RaiseAndSetIfChanged(value);
    }
}
