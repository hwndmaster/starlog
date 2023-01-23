using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.ProfileLogCodecs;

public sealed class XmlLogCodecViewModel : LogCodecViewModel
{
    private readonly XmlProfileLogCodec _xmlLogCodec;

    public XmlLogCodecViewModel(XmlProfileLogCodec logCodec)
        : base(logCodec)
    {
        _xmlLogCodec = logCodec.NotNull();
    }

    internal override void CopySettingsFrom(LogCodecViewModel logCodec)
    {
    }
}
