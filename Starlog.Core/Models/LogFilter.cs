using Genius.Atom.Infrastructure.Entities;

namespace Genius.Starlog.Core.Models;

public sealed class LogFilter : EntityBase
{
    public LogFilter(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Name { get; }
}
