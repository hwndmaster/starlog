using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Helpers;

namespace Genius.Starlog.UI.Views.ProfileFilters;

public sealed class FieldProfileFilterSettingsViewModel : ProfileFilterSettingsViewModel<FieldProfileFilter>
{
    public record FieldRecord(int Id, string Name);

    public FieldProfileFilterSettingsViewModel(FieldProfileFilter profileFilter, ILogContainer logContainer)
        : base(profileFilter)
    {
        Guard.NotNull(logContainer);

        // Members initialization:
        var fieldsContainer = logContainer.GetFields();
        Fields = new ObservableCollection<FieldRecord>(fieldsContainer.GetFields().Select(x => new FieldRecord(x.FieldId, x.FieldName)));

        // Subscriptions:
        this.WhenChanged(x => x.SelectedField)
            .Subscribe(x =>
            {
                Values.Clear();
                Values.AddRange(fieldsContainer.GetFieldValues(SelectedField.Id)
                    .Union(_profileFilter.Values)
                    .Order());
            });
        SelectedValues.WhenCollectionChanged()
            .Select(_ => Unit.Default)
            .Merge(this.WhenChanged(x => x.Exclude).Select(_ => Unit.Default))
            .Subscribe(_ =>
            {
                Name = SelectedValues.Any()
                    ? LogFilterHelpers.ProposeNameForStringList(SelectedField.Name, SelectedValues, Exclude)
                    : _profileFilter.LogFilter.Name;
            });

        // Final preparation:
        SelectedField = Fields.First(x => x.Id == _profileFilter.FieldId);
        SelectedValues.AddRange(_profileFilter.Values);
    }

    protected override void CommitChangesInternal()
    {
        _profileFilter.FieldId = SelectedField.Id;
        _profileFilter.Values = SelectedValues.ToArray();
        _profileFilter.Exclude = Exclude;
    }

    protected override void ResetChangesInternal()
    {
        SelectedField = Fields.First(x => x.Id == _profileFilter.FieldId);
        SelectedValues.Clear();
        foreach (var thread in _profileFilter.Values)
        {
            SelectedValues.Add(thread);
        }
        Exclude = _profileFilter.Exclude;
    }

    public ObservableCollection<FieldRecord> Fields { get; }
    public FieldRecord SelectedField
    {
        get => GetOrDefault(() => Fields[0]);
        set => RaiseAndSetIfChanged(value);
    }

    public ObservableCollection<string> Values { get; } = [];
    public ObservableCollection<string> SelectedValues { get; } = [];

    public bool Exclude
    {
        get => GetOrDefault(_profileFilter.Exclude);
        set => RaiseAndSetIfChanged(value);
    }
}
