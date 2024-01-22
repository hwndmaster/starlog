using System.Text.RegularExpressions;

namespace Genius.Starlog.Core.LogFiltering;

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
