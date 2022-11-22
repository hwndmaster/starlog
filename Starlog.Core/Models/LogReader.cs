using Genius.Atom.Infrastructure.Entities;

namespace Genius.Starlog.Core.Models;

public sealed class LogReader : EntityBase
{
    public LogReader(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Name { get; }
}
