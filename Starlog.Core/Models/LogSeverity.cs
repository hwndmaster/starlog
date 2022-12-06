namespace Genius.Starlog.Core.Models;

public enum LogSeverity
{
    /// <summary>
    ///   DEBUG, TRACE, STATISTICS
    /// </summary>
    Minor,

    /// <summary>
    ///   INFO
    /// </summary>
    Normal,

    /// <summary>
    ///   WARN, WARNING
    /// </summary>
    Warning,

    /// <summary>
    ///   ERR, ERROR, EXCEPTION
    /// </summary>
    Major,

    /// <summary>
    ///   FATAL
    /// </summary>
    Critical
}
