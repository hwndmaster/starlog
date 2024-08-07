using Genius.Atom.UI.Forms.Validation;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views;

public sealed class PatternValueViewModel : ViewModelBase
{
    private PatternValue _patternValue;

    public PatternValueViewModel(PatternValue patternValue)
    {
        _patternValue = patternValue.NotNull();

        // Members initialization:
        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(Pattern)));
        AddValidationRule(new IsRegexValidationRule(nameof(Pattern)), shouldValidatePropertyName: nameof(IsRegex));

        Name = patternValue.Name;
        Pattern = patternValue.Pattern;
        Type = patternValue.Type;
        IsRegex = Type == PatternType.RegularExpression;

        // Subscriptions:
        this.WhenChanged(x => x.Type)
            .Subscribe(_ => IsRegex = Type == PatternType.RegularExpression);
    }

    public PatternValue Commit()
    {
        _patternValue = new PatternValue { Id = Id, Name = Name, Type = Type, Pattern = Pattern };
        return _patternValue;
    }

    public Guid Id => _patternValue.Id;

    public string Name
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public PatternType Type
    {
        get => GetOrDefault(PatternType.RegularExpression);
        set => RaiseAndSetIfChanged(value);
    }

    public string Pattern
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public bool IsRegex
    {
        get => GetOrDefault<bool>();
        private set => RaiseAndSetIfChanged(value);
    }

    public IActionCommand DeleteCommand { get; } = new ActionCommand();
}
