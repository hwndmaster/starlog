using System.Text.RegularExpressions;
using System.Windows.Documents;
using Genius.Atom.UI.Forms.Controls.AutoGrid;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.UI.Helpers;

namespace Genius.Starlog.UI.Views;

public enum LogItemGroupingOptions
{
    NoGrouping,
    ByMessage,
    ByMessageTrimmed,
    ByField
}

public interface ILogItemViewModel : IViewModel
{
    void HandleSourceRenamed(LogSourceBase newRecord);
    int GetGroupValueId(LogItemGroupingOptions groupingOption, int? fieldId);
    IGroupableViewModel CreateGrouping(LogItemGroupingOptions groupingOption, int? fieldId);

    LogRecord Record { get; }
    IGroupableViewModel? GroupableField { get; set; }
    bool ColorizeByField { get; set; }
    int? ColorizeByFieldId { get; set; }
    string Message { get; }
    bool IsBookmarked { get; set; }
    FlowDocument Artifacts { get; }
    DynamicColumnEntriesViewModel? FieldEntries { get; set; }
    DynamicColumnEntriesViewModel? MessageParsingEntries { get; set; }
}

// TODO: Cover with unit tests
public sealed partial class LogItemViewModel : ViewModelBase, ILogItemViewModel
{
    private readonly ILogContainer _logContainer;
    private readonly Lazy<FlowDocument> _artifactsLazy;
    private string? _cachedMessageTrimmed;

    public LogItemViewModel(ILogContainer logContainer, LogRecord record, ILogArtifactsFormatter artifactsFormatter)
    {
        Guard.NotNull(artifactsFormatter);

        // Dependencies:
        _logContainer = logContainer.NotNull();

        // Members initialization:
        Record = record.NotNull();
        _artifactsLazy = new Lazy<FlowDocument>(() =>
            artifactsFormatter.CreateArtifactsDocument(Record.Source.Artifacts, Record.LogArtifacts));
    }

    public void HandleSourceRenamed(LogSourceBase newRecord)
    {
        Record = Record with { Source = newRecord };
        Source = Record.Source.DisplayName;
    }

    public int GetGroupValueId(LogItemGroupingOptions groupingOption, int? fieldId)
    {
        return groupingOption switch
        {
            LogItemGroupingOptions.NoGrouping => 0,
            LogItemGroupingOptions.ByMessage => Record.Message.GetHashCode(),
            LogItemGroupingOptions.ByMessageTrimmed
                => (_cachedMessageTrimmed = _cachedMessageTrimmed ?? TrimDigitsRegex().Replace(Record.Message, "X")).GetHashCode(),
            LogItemGroupingOptions.ByField => Record.FieldValueIndices[fieldId!.Value],
            _ => throw new InvalidOperationException("Grouping option is invalid: " + groupingOption),
        };
    }

    public IGroupableViewModel CreateGrouping(LogItemGroupingOptions groupingOption, int? fieldId)
    {
        string value = groupingOption switch
        {
            LogItemGroupingOptions.NoGrouping => string.Empty,
            LogItemGroupingOptions.ByMessage => Record.Message,
            LogItemGroupingOptions.ByMessageTrimmed => TrimDigitsRegex().Replace(Record.Message, "X"),
            LogItemGroupingOptions.ByField => _logContainer.GetFields().GetFieldValue(fieldId.NotNull().Value, Record.FieldValueIndices[fieldId!.Value]),
            _ => throw new InvalidOperationException("Grouping option is invalid: " + groupingOption),
        };

        return new DefaultGroupableViewModel(value);
    }

    public LogRecord Record { get; private set; }

    public IGroupableViewModel? GroupableField { get; set; }

    public DateTimeOffset DateTime => Record.DateTime;
    public string Level => Record.Level.Name;

    public string Source
    {
        get => GetOrDefault(Record.Source.DisplayName);
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

    /// <summary>
    ///   Matches group 1:
    ///     1.1. Begins with letters or digits.
    ///     1.2. Then contains at least one digit
    ///     1.3. Then continued with whether letters/digits/hyphens/dots/commas.
    ///   Matches group 2:
    ///     2.1. Digits
    /// </summary>
    [GeneratedRegex(@"[\w\d]+\d[\w\d\-,\.]*|\d+")]
    private static partial Regex TrimDigitsRegex();
}
