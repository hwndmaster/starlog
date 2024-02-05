namespace Genius.Starlog.Core.Models;

public interface IFileBasedProfileSettings
{
    /// <summary>
    ///   The path where the log files will be loaded from.
    /// </summary>
    string Path { get; set; }

    /// <summary>
    ///   Defines a lookup pattern for log files.
    /// </summary>
    string LogsLookupPattern { get; set; }
}
