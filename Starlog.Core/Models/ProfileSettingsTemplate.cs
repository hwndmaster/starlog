using Genius.Atom.Infrastructure.Entities;

namespace Genius.Starlog.Core.Models;

/// <summary>
///   A model of a user-defined profile.
/// </summary>
public sealed class ProfileSettingsTemplate : EntityBase
{
    /// <summary>
    ///   The name of the profile settings template.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///   The settings instance.
    /// </summary>
    public required ProfileSettingsBase Settings { get; set; }
}
