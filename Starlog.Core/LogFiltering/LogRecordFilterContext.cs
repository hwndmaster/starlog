using System.Collections.Immutable;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFiltering;

public sealed record LogRecordFilterContext(
    bool HasAnythingSpecified,
    HashSet<string> FilesSelected,
    ImmutableArray<ProfileFilterBase> FiltersSelected,
    bool ShowBookmarked,
    bool UseOrCombination,
    ImmutableArray<MessageParsing> MessageParsings)
{
    public static LogRecordFilterContext CreateEmpty() => new(false, new HashSet<string>(), ImmutableArray<ProfileFilterBase>.Empty, false, false, ImmutableArray<MessageParsing>.Empty);
}
