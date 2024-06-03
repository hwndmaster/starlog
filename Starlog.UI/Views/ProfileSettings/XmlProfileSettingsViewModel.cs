using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.ProfileSettings;

public sealed class XmlProfileSettingsViewModel : ProfileSettingsBaseViewModel
{
    private readonly XmlProfileSettings _xmlProfileSettings;

    public XmlProfileSettingsViewModel(XmlProfileSettings profileSettings)
        : base(profileSettings)
    {
        _xmlProfileSettings = profileSettings.NotNull();
    }

    public override bool CommitChanges()
    {
        // TODO: Do nothing for now
        return true;
    }

    public override void ResetForm()
    {
        // TODO: Do nothing for now
    }

    internal override void CopySettingsFrom(ProfileSettingsBaseViewModel otherProfileSettings)
    {
        // TODO: Do nothing for now
    }
}
