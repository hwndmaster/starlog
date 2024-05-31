using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.ProfileSettings;

public abstract class ProfileSettingsBaseViewModel : ViewModelBase
{
    protected ProfileSettingsBaseViewModel(ProfileSettingsBase profileSettings)
    {
        ProfileSettings = profileSettings.NotNull();
    }

    public ProfileSettingsBase ProfileSettings { get; }
    public string Name => ProfileSettings.LogCodec.Name;

    public abstract bool CommitChanges();
    public abstract void ResetForm();
    internal abstract void CopySettingsFrom(ProfileSettingsBaseViewModel logCodec);
}
