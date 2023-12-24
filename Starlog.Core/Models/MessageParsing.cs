using Genius.Atom.Infrastructure.Entities;

namespace Genius.Starlog.Core.Models;

public enum MessageParsingMethod
{
    RegEx,
    // TODO: PatternMatching
}

public sealed class MessageParsing : EntityBase
{
    public MessageParsing()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    ///   The name of the entity.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///   The parsing method.
    /// </summary>
    public MessageParsingMethod Method { get; set; }

    /// <summary>
    ///   The search pattern.
    /// </summary>
    public string Pattern { get; set; }

    /// <summary>
    ///   An array of pointers to the filters in the current profile.
    ///   This will be used to perform message parsing only of log entries,
    ///   which are satisfied with the selected filters.
    /// </summary>
    public Guid[] Filters { get; set; }
}
