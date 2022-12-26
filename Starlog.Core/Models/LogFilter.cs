using Genius.Atom.Infrastructure.Entities;

namespace Genius.Starlog.Core.Models;

/// <summary>
///   A model of a log filter.
///   Each model must have a representative as a <see cref="ProfileFilterBase" /> class with filter settings
///   and an implementation of <see cref="LogFiltering.IFilterProcessor" />.
/// </summary>
public sealed class LogFilter : EntityBase
{
    public LogFilter(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    ///   A name of the log filter.
    /// </summary>
    public string Name { get; }
}
