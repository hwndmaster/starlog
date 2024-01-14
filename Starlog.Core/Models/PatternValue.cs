using Genius.Atom.Infrastructure.Entities;

namespace Genius.Starlog.Core.Models;

/// <summary>
///   Defines a named pattern value.
/// </summary>
/// <remarks>
///   If <seealso cref="PatternValue.Type"/> is <seealso cref="PatternType.RegularExpression"/>, then each
///   regular expression must contain the following groups, known by the system:
///   - datetime
///   - level
///   - thread
///   - logger
///   - message.
///   If it is <seealso cref="PatternType.MaskPattern"/>, then the pattern should must contain
///   the following entries:
///   - %d or %{datetime} for DateTime
///   - %l or %{level} for level
///   - %t or %{thread} for Thread
///   - %c or %{logger} for logger
///   - %m or %{message} for message
///   - %s for any word.
/// </remarks>
public sealed class PatternValue : EntityBase
{
    public PatternValue()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    ///   A name of the pattern.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///   A type of the pattern.
    /// </summary>
    public required PatternType Type { get; set; }

    /// <summary>
    ///   The pattern.
    /// </summary>
    public required string Pattern { get; set; }
}
