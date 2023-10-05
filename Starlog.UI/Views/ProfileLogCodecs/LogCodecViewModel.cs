using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.ProfileLogCodecs;

// TODO: Cover with unit tests
public abstract class LogCodecViewModel : ViewModelBase
{
    protected LogCodecViewModel(ProfileLogCodecBase logCodec)
    {
        ProfileLogCodec = logCodec.NotNull();
    }

    public ProfileLogCodecBase ProfileLogCodec { get; }
    public string Name => ProfileLogCodec.LogCodec.Name;

    internal abstract void CopySettingsFrom(LogCodecViewModel logCodec);
}
