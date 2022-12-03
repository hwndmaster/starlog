using System.Collections.ObjectModel;
using Genius.Atom.UI.Forms.Validation;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.ViewModels;

public record PlainTextLogReaderLineRegex(string Name, string Regex)
{
    public override string ToString() => Regex;
}

public sealed class PlainTextLogReaderViewModel : LogReaderViewModel
{
    private readonly PlainTextProfileLogReader _plainTextLogReader;

    public PlainTextLogReaderViewModel(PlainTextProfileLogReader logReader)
        : base(logReader)
    {
        _plainTextLogReader = logReader.NotNull();

        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(LineRegex)));
        AddValidationRule(new IsRegexValidationRule(nameof(LineRegex)));

        LineRegexes.Add(new PlainTextLogReaderLineRegex(
            "LEVEL DATETIME [Thread] Logger - Message",
            @"(?<level>\w+)\s(?<datetime>[\d\-:\.]+\s[\d\-:\.]+)\s\[(?<thread>\w+)\]\s(?<logger>\w+)\s-\s(?<message>.+)"));
    }

    public ObservableCollection<PlainTextLogReaderLineRegex> LineRegexes { get; } = new();

    public string LineRegex
    {
        get => GetOrDefault(_plainTextLogReader.LineRegex);
        set => RaiseAndSetIfChanged(value, (_, v) => {
                if (!PropertyHasErrors(nameof(LineRegex)))
                {
                    _plainTextLogReader.LineRegex = v;
                }
            });
    }
}
