using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Helpers;

namespace Genius.Starlog.UI.Views.ProfileFilters;

// TODO: Cover with unit tests
public sealed class FieldProfileFilterSettingsViewModel : ProfileFilterSettingsViewModel<FieldProfileFilter>
{
    public FieldProfileFilterSettingsViewModel(FieldProfileFilter profileFilter, ILogContainer logContainer)
        : base(profileFilter)
    {
        Guard.NotNull(logContainer);

        // Members initialization:
        Values = logContainer.GetFields().GetFieldValues(profileFilter.FieldId)
            .Union(profileFilter.Values)
            .Order()
            .ToImmutableArray();
        SelectedValues = new ObservableCollection<string>(profileFilter.Values);

        // Subscriptions:
        SelectedValues.WhenCollectionChanged()
            .Select(_ => Unit.Default)
            .Merge(this.WhenChanged(x => x.Exclude).Select(_ => Unit.Default))
            .Subscribe(_ =>
            {
                Name = SelectedValues.Any()
                    ? LogFilterHelpers.ProposeNameForStringList(logContainer.GetFields().GetFieldName(profileFilter.FieldId), SelectedValues, Exclude)
                    : profileFilter.LogFilter.Name;
            });
    }

    protected override void CommitChangesInternal()
    {
        _profileFilter.Values = SelectedValues.ToArray();
        _profileFilter.Exclude = Exclude;
    }

    protected override void ResetChangesInternal()
    {
        SelectedValues.Clear();
        foreach (var thread in _profileFilter.Values)
        {
            SelectedValues.Add(thread);
        }

        Exclude = _profileFilter.Exclude;
    }

    public ImmutableArray<string> Values { get; }
    public ObservableCollection<string> SelectedValues { get; }

    public bool Exclude
    {
        get => GetOrDefault(_profileFilter.Exclude);
        set => RaiseAndSetIfChanged(value);
    }
}
