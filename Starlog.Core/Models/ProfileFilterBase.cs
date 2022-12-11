using Genius.Atom.Infrastructure.Entities;

namespace Genius.Starlog.Core.Models;

public abstract class ProfileFilterBase : EntityBase
{
    protected ProfileFilterBase(LogFilter logFilter)
    {
        Id = Guid.NewGuid();
        LogFilter = logFilter;
        Name = logFilter.Name;
    }

    public string Name { get; set; }
    public LogFilter LogFilter { get; }
}
