using Genius.Atom.UI.Forms;
using Genius.Atom.UI.Forms.Validation;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.ViewModels;

internal sealed class RegexValueViewModel : ViewModelBase
{
    public RegexValueViewModel(StringValue stringValue)
    {
        Guard.NotNull(stringValue);

        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(Regex)));
        AddValidationRule(new IsRegexValidationRule(nameof(Regex)));

        Name = stringValue.Name;
        Regex = stringValue.Value;
    }

    public string Name
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public string Regex
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public IActionCommand DeleteCommand { get; } = new ActionCommand();
}
