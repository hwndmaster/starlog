using System.Collections.ObjectModel;
using Genius.Atom.UI.Forms.Validation;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.UI.Views.ProfileLogCodecs;

// TODO: Cover with unit tests
public sealed class PlainTextLogCodecViewModel : LogCodecViewModel
{
    private readonly PlainTextProfileLogCodec _plainTextLogCodec;

    public PlainTextLogCodecViewModel(PlainTextProfileLogCodec logCodec, ISettingsQueryService settingsQuery)
        : base(logCodec)
    {
        // Dependencies:
        _plainTextLogCodec = logCodec.NotNull();

        // Members initialization:
        AddValidationRule(new NotNullValidationRule(nameof(LinePattern)));

        var patterns = settingsQuery.NotNull().Get().PlainTextLogCodecLinePatterns;
        foreach (var pattern in patterns)
        {
            LinePatterns.Add(new PatternValueViewModel(pattern));
        }
        LinePattern = LinePatterns.FirstOrDefault(x => x.Id == _plainTextLogCodec.LinePatternId)
            ?? LinePatterns[0];

        // Subscriptions:
        this.WhenChanged(x => x.LinePattern)
            .Subscribe(x => {
                if (!PropertyHasErrors(nameof(LinePattern)))
                    _plainTextLogCodec.LinePatternId = LinePattern.Id;
            });
    }

    internal override void CopySettingsFrom(LogCodecViewModel logCodec)
    {
        if (logCodec is not PlainTextLogCodecViewModel settings)
            return;

        LinePattern = LinePatterns.FirstOrDefault(x => x.Id == settings.LinePattern.Id) ?? LinePatterns[0];
        _plainTextLogCodec.LinePatternId = LinePattern.Id;
    }

    public ObservableCollection<PatternValueViewModel> LinePatterns { get; } = new();

    public PatternValueViewModel LinePattern
    {
        get => GetOrDefault<PatternValueViewModel>();
        set => RaiseAndSetIfChanged(value);
    }
}
