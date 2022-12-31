using System.Reactive.Linq;
using Genius.Atom.UI.Forms.Validation;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Helpers;

namespace Genius.Starlog.UI.Views.ProfileFilters;

public sealed class MessageProfileFilterSettingsViewModel : ProfileFilterSettingsViewModel<MessageProfileFilter>
{
    public MessageProfileFilterSettingsViewModel(MessageProfileFilter profileFilter, ILogContainer logContainer)
        : base(profileFilter)
    {
        Guard.NotNull(logContainer);

        // Members initialization:
        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(Pattern)));
        AddValidationRule(new IsRegexValidationRule(nameof(Pattern)), shouldValidatePropertyName: nameof(IsRegex));
        ResetChangesInternal();

        // Subscriptions:
        this.WhenChanged(x => x.Pattern)
            .Subscribe(_ => Name = LogFilterHelpers.LimitNameLength("Contains '" + Pattern + "'"));
    }

    protected override void CommitChangesInternal()
    {
        _profileFilter.Pattern = Pattern;
        _profileFilter.IsRegex = IsRegex;
        _profileFilter.MatchCasing = MatchCasing;
        _profileFilter.IncludeArtifacts = IncludeArtifacts;
    }

    protected override void ResetChangesInternal()
    {
        Pattern = _profileFilter.Pattern;
        IsRegex = _profileFilter.IsRegex;
        MatchCasing = _profileFilter.MatchCasing;
        IncludeArtifacts = _profileFilter.IncludeArtifacts;
    }

    public string Pattern
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public bool IsRegex
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public bool MatchCasing
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public bool IncludeArtifacts
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }
}
