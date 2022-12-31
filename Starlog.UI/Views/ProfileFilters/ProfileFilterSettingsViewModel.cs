using Genius.Atom.UI.Forms.Validation;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.ProfileFilters;

public interface IProfileFilterSettingsViewModel : IViewModel
{
    void CommitChanges();
    void ResetChanges();

    ProfileFilterBase ProfileFilter { get; }
    string Name { get; set; }
}

public abstract class ProfileFilterSettingsViewModel<TProfileFilter> : ViewModelBase, IProfileFilterSettingsViewModel
    where TProfileFilter : ProfileFilterBase
{
    protected TProfileFilter _profileFilter;

    protected ProfileFilterSettingsViewModel(TProfileFilter profileFilter)
    {
        _profileFilter = profileFilter.NotNull();

        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(Name)));
    }

    public void CommitChanges()
    {
        ProfileFilter.Name = Name;

        CommitChangesInternal();
    }

    public void ResetChanges()
    {
        Name = ProfileFilter.Name;

        ResetChangesInternal();
    }

    protected abstract void CommitChangesInternal();
    protected abstract void ResetChangesInternal();

    public ProfileFilterBase ProfileFilter => _profileFilter;

    public string Name
    {
        get => GetOrDefault(ProfileFilter.Name);
        set => RaiseAndSetIfChanged(value, (old, @new) => ProfileFilter.Name = @new);
    }
}
