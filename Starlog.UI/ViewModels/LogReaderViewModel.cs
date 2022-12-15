
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.ViewModels;

public abstract class LogReaderViewModel : ViewModelBase
{
    protected LogReaderViewModel(ProfileLogReaderBase logReader)
    {
        LogReader = logReader.NotNull();
    }

    public ProfileLogReaderBase LogReader { get; }
    public string Name => LogReader.LogReader.Name;
}
