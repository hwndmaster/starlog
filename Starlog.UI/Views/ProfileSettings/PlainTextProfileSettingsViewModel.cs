using System.Collections.ObjectModel;
using Genius.Atom.UI.Forms.Validation;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.UI.Views.ProfileSettings;

public sealed class PlainTextProfileSettingsViewModel : ProfileSettingsBaseViewModel
{
    private readonly PlainTextProfileSettings _plainTextProfileSettings;

    public PlainTextProfileSettingsViewModel(PlainTextProfileSettings profileSettings, ISettingsQueryService settingsQuery)
        : base(profileSettings)
    {
        // Dependencies:
        _plainTextProfileSettings = profileSettings.NotNull();

        // Members initialization:
        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(DateTimeFormat)));
        AddValidationRule(new NotNullValidationRule(nameof(LinePattern)));
        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(LogsLookupPattern)));
        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(Paths)));
        AddValidationRule(new PathExistsValidationRule(nameof(Paths), acceptMultiplePaths: true));

        var patterns = settingsQuery.NotNull().Get().PlainTextLogCodecLinePatterns;
        foreach (var pattern in patterns)
        {
            LinePatterns.Add(new PatternValueViewModel(pattern));
        }

        ResetForm();
    }

    public override bool CommitChanges()
    {
        Validate();
        if (HasErrors)
            return false;

        _plainTextProfileSettings.DateTimeFormat = DateTimeFormat;
        _plainTextProfileSettings.FileArtifactLinesCount = FileArtifactLinesCount;
        _plainTextProfileSettings.LinePatternId = LinePattern.Id;
        _plainTextProfileSettings.LogsLookupPattern = LogsLookupPattern;
        _plainTextProfileSettings.Paths = Paths.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return true;
    }


    public override void ResetForm()
    {
        DateTimeFormat = _plainTextProfileSettings.DateTimeFormat;
        FileArtifactLinesCount = _plainTextProfileSettings.FileArtifactLinesCount;
        LinePattern = LinePatterns.FirstOrDefault(x => x.Id == _plainTextProfileSettings.LinePatternId)
            ?? LinePatterns[0];
        LogsLookupPattern = _plainTextProfileSettings.LogsLookupPattern;
        Paths = string.Join(',', _plainTextProfileSettings.Paths);
    }

    internal override void CopySettingsFrom(ProfileSettingsBaseViewModel otherProfileSettings)
    {
        if (otherProfileSettings is not PlainTextProfileSettingsViewModel settings)
            return;

        DateTimeFormat = settings.DateTimeFormat;
        FileArtifactLinesCount = settings.FileArtifactLinesCount;
        LinePattern = LinePatterns.FirstOrDefault(x => x.Id == settings.LinePattern.Id) ?? LinePatterns[0];
        LogsLookupPattern = settings.LogsLookupPattern;
        Paths = string.IsNullOrEmpty(settings.Paths) ? Paths : settings.Paths;
    }

    public string DateTimeFormat
    {
        get => GetOrDefault(string.Empty);
        set => RaiseAndSetIfChanged(value);
    }

    public int FileArtifactLinesCount
    {
        get => GetOrDefault(0);
        set => RaiseAndSetIfChanged(value);
    }

    public ObservableCollection<PatternValueViewModel> LinePatterns { get; } = new();

    public PatternValueViewModel LinePattern
    {
        get => GetOrDefault<PatternValueViewModel>();
        set => RaiseAndSetIfChanged(value);
    }

    public string LogsLookupPattern
    {
        get => GetOrDefault(PlainTextProfileSettings.DefaultLogsLookupPattern);
        set => RaiseAndSetIfChanged(value);
    }

    public string Paths
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }
}
