using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.ProfileLogReaders;

public sealed class XmlLogReaderViewModel : LogReaderViewModel
{
    private readonly XmlProfileLogRead _xmlLogReader;

    public XmlLogReaderViewModel(XmlProfileLogRead logReader)
        : base(logReader)
    {
        _xmlLogReader = logReader.NotNull();
    }
}
