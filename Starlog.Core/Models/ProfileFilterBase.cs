using Genius.Atom.Infrastructure.Entities;

namespace Genius.Starlog.Core.Models;

/// <summary>
///   The base class for all user-defined filters and automatically created 'quick' filters in the profile.
/// </summary>
public abstract class ProfileFilterBase : EntityBase
{
    protected ProfileFilterBase(LogFilter logFilter)
    {
        Id = Guid.NewGuid();
        LogFilter = logFilter;
        Name = logFilter.Name;
    }

    /// <summary>
    ///   The name of the filter.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///   Points to a specific <see cref="LogFilter"> registered in the system,
    ///   which indicates the type of the filter.
    /// </summary>
    public LogFilter LogFilter { get; }
}
