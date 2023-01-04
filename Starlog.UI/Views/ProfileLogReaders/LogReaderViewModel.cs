
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.ProfileLogReaders;

public abstract class LogReaderViewModel : ViewModelBase
{
    protected LogReaderViewModel(ProfileLogReadBase logReader)
    {
        LogReader = logReader.NotNull();
    }

    public ProfileLogReadBase LogReader { get; }
    public string Name => LogReader.LogReader.Name;

    internal abstract void CopySettingsFrom(LogReaderViewModel logReader);
}
