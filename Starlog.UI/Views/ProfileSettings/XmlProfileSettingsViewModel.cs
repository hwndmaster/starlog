using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.ProfileSettings;

public sealed class XmlProfileSettingsViewModel : ProfileSettingsBaseViewModel
{
    private readonly XmlProfileSettings _xmlLogCodec;

    public XmlProfileSettingsViewModel(XmlProfileSettings logCodec)
        : base(logCodec)
    {
        _xmlLogCodec = logCodec.NotNull();
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

    internal override void CopySettingsFrom(ProfileSettingsBaseViewModel logCodec)
    {
        // TODO: Do nothing for now
    }
}
