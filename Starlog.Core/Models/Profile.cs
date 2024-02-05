using System.Text.Json.Serialization;
using Genius.Atom.Infrastructure.Entities;

namespace Genius.Starlog.Core.Models;

/// <summary>
///   A model of a user-defined profile.
/// </summary>
public sealed class Profile : EntityBase
{
    public static readonly Guid AnonymousProfileId = new("00000000-0000-0000-0000-000000000001");

    /// <summary>
    ///   The profile name.
    /// </summary>
    public required string Name { get; set; }

    [Obsolete("Used for backwards compatibility only. To be removed in the next major version.")]
    public string? Path { get; set; }

    /// <summary>
    ///   Gets a boolean value which indicates whether this profile is anonymous or not.
    /// </summary>
    [JsonIgnore]
    public bool IsAnonymous => Id == AnonymousProfileId;

    /// <summary>
    ///   The profile settings with the log codec in it, which defines the strategy of how the logs are being read.
    /// </summary>
    public required ProfileSettingsBase Settings { get; set; }

    /// <summary>
    ///   The user-defined filters.
    /// </summary>
    public IList<ProfileFilterBase> Filters { get; set; } = new List<ProfileFilterBase>();

    /// <summary>
    ///   The user-defined message parsings.
    /// </summary>
    public IList<MessageParsing> MessageParsings { get; set; } = new List<MessageParsing>();
}
