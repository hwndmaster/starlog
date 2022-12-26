using Genius.Atom.Infrastructure.Entities;

namespace Genius.Starlog.Core.Models;

/// <summary>
///   A model of a user-defined profile.
/// </summary>
public sealed class Profile : EntityBase
{
    /// <summary>
    ///   The profile name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///   The path where the log files will be loaded from.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    ///   The log reader which defines the strategy of how the logs are being read.
    /// </summary>
    public required ProfileLogReadBase LogReader { get; set; }

    /// <summary>
    ///   The user-defined filters.
    /// </summary>
    public IList<ProfileFilterBase> Filters { get; set; } = new List<ProfileFilterBase>();

    /// <summary>
    ///   Indicates the number of how many lines in each log file are dedicated for the file artifacts.
    ///   Such as command line arguments, the time when file logging has been started, etc.
    /// </summary>
    public int FileArtifactLinesCount { get; set; } = 0;
}
