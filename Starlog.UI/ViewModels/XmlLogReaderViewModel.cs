using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.ViewModels;

public sealed class XmlLogReaderViewModel : LogReaderViewModel
{
    private readonly XmlProfileLogReader _xmlLogReader;

    public XmlLogReaderViewModel(XmlProfileLogReader logReader)
        : base(logReader)
    {
        _xmlLogReader = logReader.NotNull();
    }
}
