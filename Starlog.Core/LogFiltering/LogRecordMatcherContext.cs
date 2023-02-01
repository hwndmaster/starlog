using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFiltering;

public sealed record LogRecordMatcherContext(LogRecordFilterContext Filter, LogRecordSearchContext Search);

public sealed record LogRecordSearchContext(
    bool HasAnythingSpecified,
    bool MessageSearchIncluded,
    string SearchText,
    Regex? MessageSearchRegex,
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo)
{
    public static LogRecordSearchContext CreateEmpty() => new(false, false, string.Empty, null, null, null);
}

public sealed record LogRecordFilterContext(
    bool HasAnythingSpecified,
    HashSet<string> FilesSelected,
    ImmutableArray<ProfileFilterBase> FiltersSelected,
    bool ShowBookmarked)
{
    public static LogRecordFilterContext CreateEmpty() => new(false, new HashSet<string>(), ImmutableArray<ProfileFilterBase>.Empty, false);
}
