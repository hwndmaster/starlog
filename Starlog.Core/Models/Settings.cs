namespace Genius.Starlog.Core.Models;

/// <summary>
///   The model for all app settings.
/// </summary>
public sealed class Settings
{
    /// <summary>
    ///   Indicates whether the application should automatically load previously opened profile.
    ///   Works in conjunction with <see cref="AutoLoadProfile" />.
    /// </summary>
    public bool AutoLoadPreviouslyOpenedProfile { get; set; }

    /// <summary>
    ///   The identifier of the profile to be automatically loaded when the application starts.
    ///   Works in conjunction with <see cref="AutoLoadPreviouslyOpenedProfile" />.
    /// </summary>
    public Guid? AutoLoadProfile { get; set; }

    /// <summary>
    ///   A list of pattern templates which can be used in profiles with log codec type "Plain Text".
    /// </summary>
    public ICollection<PatternValue> PlainTextLogCodecLinePatterns { get; set; } = Array.Empty<PatternValue>();
}
