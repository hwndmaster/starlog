using System.Collections.ObjectModel;
using Genius.Atom.UI.Forms.Validation;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.UI.Views.ProfileLogReaders;

public sealed record PlainTextLogReaderLineRegex(string Name, string Regex)
{
    public override string ToString() => Regex;
}

public sealed class PlainTextLogReaderViewModel : LogReaderViewModel
{
    private readonly PlainTextProfileLogRead _plainTextLogReader;

    public PlainTextLogReaderViewModel(PlainTextProfileLogRead logReader, ISettingsQueryService settingsQuery)
        : base(logReader)
    {
        _plainTextLogReader = logReader.NotNull();

        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(LineRegex)));
        AddValidationRule(new IsRegexValidationRule(nameof(LineRegex)));

        var templates = settingsQuery.NotNull().Get().PlainTextLogReaderLineRegexes;
        foreach (var template in templates)
        {
            LineRegexes.Add(new PlainTextLogReaderLineRegex(template.Name, template.Value));
        }
    }

    internal override void CopySettingsFrom(LogReaderViewModel logReader)
    {
        if (logReader is not PlainTextLogReaderViewModel settings)
            return;

        LineRegex = settings.LineRegex;
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
