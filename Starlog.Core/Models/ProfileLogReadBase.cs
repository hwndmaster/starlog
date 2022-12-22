namespace Genius.Starlog.Core.Models;

public abstract class ProfileLogReadBase
{
    protected ProfileLogReadBase(LogReader logReader)
    {
        LogReader = logReader;
    }

    public LogReader LogReader { get; }
}
