namespace Genius.Starlog.Core.Models;

/// <summary>
///   The base class for log readers defined in the profile with supplemental settings,
///   depending on a log reader type.
/// </summary>
public abstract class ProfileLogReadBase
{
    protected ProfileLogReadBase(LogReader logReader)
    {
        LogReader = logReader;
    }

    /// <summary>
    ///   Points to a log reader, registered in the system.
    /// </summary>
    public LogReader LogReader { get; }
}
