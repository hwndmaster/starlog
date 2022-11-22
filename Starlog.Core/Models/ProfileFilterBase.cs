using Genius.Atom.Infrastructure.Entities;

namespace Genius.Starlog.Core.Models;

public abstract class ProfileFilterBase : EntityBase
{
    protected ProfileFilterBase(LogFilter logFilter)
    {
        LogFilter = logFilter;
    }

    public LogFilter LogFilter { get; }
}
