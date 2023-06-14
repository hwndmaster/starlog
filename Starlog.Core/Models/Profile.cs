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

    /// <summary>
    ///   The path where the log files will be loaded from.
    /// </summary>
    public required string Path { get; set; }

    public bool IsAnonymous => Id == AnonymousProfileId;

    /// <summary>
    ///   The log codec which defines the strategy of how the logs are being read.
    /// </summary>
    public required ProfileSettings Settings { get; set; }

    /// <summary>
    ///   The user-defined filters.
    /// </summary>
    public IList<ProfileFilterBase> Filters { get; set; } = new List<ProfileFilterBase>();
}
