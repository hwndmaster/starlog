using System.Text.Json.Serialization;

namespace Genius.Starlog.Core.Models;

/// <summary>
///   The base class for profile settings.
/// </summary>
public abstract class ProfileSettingsBase
{
    protected ProfileSettingsBase(LogCodec logCodec)
    {
        LogCodec = logCodec;
    }

    /// <summary>
    ///   Points to a log codec, registered in the system.
    /// </summary>
    public LogCodec LogCodec { get; }

    [JsonIgnore]
    public abstract string Source { get; }

    public ProfileSettingsBase Clone()
    {
        return CloneInternal();
    }

    internal abstract ProfileSettingsBase CloneInternal();
}
