namespace Genius.Starlog.Core.Models;

public abstract class ProfileLogReaderBase
{
    protected ProfileLogReaderBase(LogReader logReader)
    {
        LogReader = logReader;
    }

    public LogReader LogReader { get; }
}
