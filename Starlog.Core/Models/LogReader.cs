using Genius.Atom.Infrastructure.Entities;

namespace Genius.Starlog.Core.Models;

/// <summary>
///   A model of a log filter.
///   Each model must have a representative as a <see cref="ProfileLogReadBase" /> class with reader settings
///   and an implementation of <see cref="LogReading.ILogReaderProcessor" />.
/// </summary>
public sealed class LogReader : EntityBase
{
    public LogReader(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    ///   A name of the log reader.
    /// </summary>
    public string Name { get; }
}
