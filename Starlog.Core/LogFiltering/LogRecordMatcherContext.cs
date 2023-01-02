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
    DateTimeOffset? DateTo);

public sealed record LogRecordFilterContext(
    bool HasAnythingSpecified,
    HashSet<string> FilesSelected,
    ImmutableArray<ProfileFilterBase> FiltersSelected,
    bool ShowBookmarked);
