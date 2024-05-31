namespace Genius.Starlog.Core.Models;

/// <summary>
///   The log codec settings for the profile with Plain Text log codec type.
/// </summary>
public sealed class PlainTextProfileSettings : ProfileSettingsBase, IFileBasedProfileSettings
{
    public const string CodecName = "Plain Text";
    public const string DefaultDateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
    public const string DefaultLogsLookupPattern = "*.*";

    public PlainTextProfileSettings(LogCodec logCodec)
        : base(logCodec)
    {
    }

    internal override ProfileSettingsBase CloneInternal()
    {
        return new PlainTextProfileSettings(LogCodec)
        {
            LinePatternId = LinePatternId,
            Path = Path,
            FileArtifactLinesCount = FileArtifactLinesCount,
            LogsLookupPattern = LogsLookupPattern,
            DateTimeFormat = DateTimeFormat
        };
    }

    public override string Source => Path;

    /// <summary>
    ///   The pattern which is used to parse each line of the log file.
    /// </summary>
    public Guid LinePatternId { get; set; }

    /// <summary>
    ///   The path where the log files will be loaded from.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    ///   Indicates the number of how many lines in each log file are dedicated for the file artifacts.
    ///   Such as command line arguments, the time when file logging has been started, etc.
    /// </summary>
    public int FileArtifactLinesCount { get; set; }

    /// <summary>
    ///   Defines a lookup pattern for log files.
    /// </summary>
    public string LogsLookupPattern { get; set; } = DefaultLogsLookupPattern;

    /// <summary>
    ///   Defines the log item's date time format.
    /// </summary>
    public string DateTimeFormat { get; set; } = DefaultDateTimeFormat;
}
