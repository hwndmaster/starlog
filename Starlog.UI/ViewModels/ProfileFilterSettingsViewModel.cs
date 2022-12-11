using Genius.Atom.UI.Forms;
using Genius.Atom.UI.Forms.Validation;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.ViewModels;

public interface IProfileFilterSettingsViewModel : IViewModel
{
    ProfileFilterBase ProfileFilter { get; }
    string Name { get; set; }
}

public abstract class ProfileFilterSettingsViewModel : ViewModelBase, IProfileFilterSettingsViewModel
{
    private const int MaxNameLength = 50;

    protected ProfileFilterSettingsViewModel(ProfileFilterBase profileFilter)
    {
        ProfileFilter = profileFilter.NotNull();

        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(Name)));
    }

    protected static string LimitNameLength(string name)
    {
        if (name.Length > MaxNameLength)
        {
            return name[..(MaxNameLength - 1)] + "…";
        }

        return name;
    }

    public ProfileFilterBase ProfileFilter { get; }

    public string Name
    {
        get => GetOrDefault(ProfileFilter.Name);
        set => RaiseAndSetIfChanged(value, (old, @new) => ProfileFilter.Name = @new);
    }
}
