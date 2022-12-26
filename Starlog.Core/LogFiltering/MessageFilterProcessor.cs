using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFiltering;

public sealed class MessageFilterProcessor : IFilterProcessor
{
    private static readonly ConcurrentDictionary<int, Regex> _regexes = new();

    public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log)
    {
        var filter = (MessageProfileFilter)profileFilter;

        if (filter.IsRegex)
        {
            var regex = CreateRegex(filter);
            return regex.IsMatch(log.Message)
                || (filter.IncludeArtifacts && log.LogArtifacts is not null && regex.IsMatch(log.LogArtifacts));
        }

        var stringComparison = filter.MatchCasing
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return log.Message.Contains(filter.Pattern, stringComparison)
            || (filter.IncludeArtifacts && log.LogArtifacts?.Contains(filter.Pattern, stringComparison) == true);
    }

    private static Regex CreateRegex(MessageProfileFilter filter)
    {
        var patternHash = HashCode.Combine(filter.Pattern, filter.MatchCasing);

        return _regexes.GetOrAdd(patternHash, _ =>
        {
            var options = RegexOptions.None;

            if (!filter.MatchCasing)
            {
                options |= RegexOptions.IgnoreCase;
            }

            return new Regex(filter.Pattern, options);
        });
    }
}
