using System.Windows.Documents;
using Genius.Atom.UI.Forms.Controls.AutoGrid;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.UI.Helpers;

namespace Genius.Starlog.UI.Views;

public interface ILogItemViewModel : IViewModel
{
    void HandleSourceRenamed(LogSourceBase newRecord);

    LogRecord Record { get; }
    bool ColorizeByField { get; set; }
    int? ColorizeByFieldId { get; set; }
    string Message { get; }
    int? GroupedFieldId { get; set; }
    string? GroupedFieldValue { get; }
    bool IsBookmarked { get; set; }
    FlowDocument Artifacts { get; }
    DynamicColumnEntriesViewModel? FieldEntries { get; set; }
    DynamicColumnEntriesViewModel? MessageParsingEntries { get; set; }
}

// TODO: Cover with unit tests
public sealed class LogItemViewModel : ViewModelBase, ILogItemViewModel
{
    private readonly Lazy<FlowDocument> _artifactsLazy;

    public LogItemViewModel(ILogContainer logContainer, LogRecord record, ILogArtifactsFormatter artifactsFormatter)
    {
        Guard.NotNull(artifactsFormatter);
        Guard.NotNull(logContainer);

        // Members initialization:
        Record = record.NotNull();
        _artifactsLazy = new Lazy<FlowDocument>(() =>
            artifactsFormatter.CreateArtifactsDocument(Record.Source.Artifacts, Record.LogArtifacts));

        // Subscriptions:
        this.WhenChanged(x => x.GroupedFieldId).Subscribe(groupFieldId =>
        {
            if (groupFieldId is null)
            {
                GroupedFieldValue = null;
            }
            else
            {
                var value = logContainer.GetFields().GetFieldValue(groupFieldId.Value, Record.FieldValueIndices[groupFieldId.Value]);
                GroupedFieldValue = value;
            }
        });
    }

    public void HandleSourceRenamed(LogSourceBase newRecord)
    {
        Record = Record with { Source = newRecord };
        Source = Record.Source.DisplayName;
    }

    public LogRecord Record { get; private set; }

    public DateTimeOffset DateTime => Record.DateTime;
    public string Level => Record.Level.Name;

    public string Source
    {
        get => GetOrDefault(Record.Source.DisplayName);
        set => RaiseAndSetIfChanged(value);
    }

    public int? GroupedFieldId
    {
        get => GetOrDefault<int?>();
        set => RaiseAndSetIfChanged(value);
    }

    public string? GroupedFieldValue
    {
        get => GetOrDefault<string?>();
        set => RaiseAndSetIfChanged(value);
    }

    public string Message => Record.Message;
    public string? ArtifactsIcon => string.IsNullOrEmpty(Record.LogArtifacts) ? null : "Note32";

    public bool IsBookmarked
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public bool ColorizeByField
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value, (_, __) => OnPropertyChanged(string.Empty));
    }

    public int? ColorizeByFieldId
    {
        get => GetOrDefault<int?>();
        set => RaiseAndSetIfChanged(value);
    }

    public DynamicColumnEntriesViewModel? FieldEntries { get; set; }
    public DynamicColumnEntriesViewModel? MessageParsingEntries { get; set; }

    public FlowDocument Artifacts => _artifactsLazy.Value;
}
