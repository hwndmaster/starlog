using Genius.Atom.Infrastructure.Entities;

namespace Genius.Starlog.Core.Models;

/// <summary>
///   A model of a log codec.
///   Each model must have a representative as a <see cref="ProfileLogCodecBase" /> class with codec settings
///   and an implementation of <see cref="LogReading.ILogCodecProcessor" />.
/// </summary>
public sealed class LogCodec : EntityBase
{
    public LogCodec(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    ///   A name of the log codec.
    /// </summary>
    public string Name { get; }
}
