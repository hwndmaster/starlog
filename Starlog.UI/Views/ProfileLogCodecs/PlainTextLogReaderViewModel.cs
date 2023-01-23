using System.Collections.ObjectModel;
using Genius.Atom.UI.Forms.Validation;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.UI.Views.ProfileLogCodecs;

public sealed record PlainTextLogCodecLineRegex(string Name, string Regex)
{
    public override string ToString() => Regex;
}

public sealed class PlainTextLogCodecViewModel : LogCodecViewModel
{
    private readonly PlainTextProfileLogCodec _plainTextLogCodec;

    public PlainTextLogCodecViewModel(PlainTextProfileLogCodec logCodec, ISettingsQueryService settingsQuery)
        : base(logCodec)
    {
        _plainTextLogCodec = logCodec.NotNull();

        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(LineRegex)));
        AddValidationRule(new IsRegexValidationRule(nameof(LineRegex)));

        var templates = settingsQuery.NotNull().Get().PlainTextLogCodecLineRegexes;
        foreach (var template in templates)
        {
            LineRegexes.Add(new PlainTextLogCodecLineRegex(template.Name, template.Value));
        }
    }

    internal override void CopySettingsFrom(LogCodecViewModel logCodec)
    {
        if (logCodec is not PlainTextLogCodecViewModel settings)
            return;

        LineRegex = settings.LineRegex;
    }

    public ObservableCollection<PlainTextLogCodecLineRegex> LineRegexes { get; } = new();

    public string LineRegex
    {
        get => GetOrDefault(_plainTextLogCodec.LineRegex);
        set => RaiseAndSetIfChanged(value, (_, v) => {
                if (!PropertyHasErrors(nameof(LineRegex)))
                {
                    _plainTextLogCodec.LineRegex = v;
                }
            });
    }
}
