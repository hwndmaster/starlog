using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.ViewModels;

public sealed class LogReaderViewModel : ViewModelBase
{
    public LogReaderViewModel(ProfileLogReaderBase logReader)
    {
        LogReader = logReader.NotNull();
    }

    public ProfileLogReaderBase LogReader { get; }
    public string Name => LogReader.LogReader.Name;
}
