namespace Genius.Starlog.Core.Models;

/// <summary>
///   A model of a user-defined profile.
/// </summary>
public sealed class ProfileSettings
{
    /// <summary>
    ///   The log codec which defines the strategy of how the logs are being read.
    /// </summary>
    public required ProfileLogCodecBase LogCodec { get; set; }

    /// <summary>
    ///   Indicates the number of how many lines in each log file are dedicated for the file artifacts.
    ///   Such as command line arguments, the time when file logging has been started, etc.
    /// </summary>
    public int FileArtifactLinesCount { get; set; } = 0;
}
