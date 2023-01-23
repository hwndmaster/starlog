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
    public bool AutoLoadPreviouslyOpenedProfile { get; set; } = false;

    /// <summary>
    ///   The identifier of the profile to be automatically loaded when the application starts.
    ///   Works in conjunction with <see cref="AutoLoadPreviouslyOpenedProfile" />.
    /// </summary>
    public Guid? AutoLoadProfile { get; set; } = null;

    /// <summary>
    ///   A list of regular expression templates which can be used in profiles with log codec type "Plain Text".
    /// </summary>
    /// <remarks>
    ///   Each regular expression should contain the following groups, known by the system:
    ///   - level
    ///   - datetime
    ///   - thread
    ///   - logger
    ///   - message
    /// </remarks>
    public ICollection<SettingStringValue> PlainTextLogCodecLineRegexes { get; set; } = Array.Empty<SettingStringValue>();
}
