using System.Text.RegularExpressions;

namespace Genius.Starlog.Core.LogReading.PlainTextLogCodecParsers;

internal sealed class PlainTextLogCodecLineRegexParser : IPlainTextLogCodecLineParser
{
    /// <summary>
    ///   4 means datetime + message + level + zero-indexed group
    /// </summary>
    private const int FIXED_GROUPS_COUNT = 4;
    private const string DEFAULT_LEVEL = "INFO";

    private readonly Regex _regex;

    public PlainTextLogCodecLineRegexParser(string pattern)
    {
        _regex = new(pattern);
    }

    public ParsedLine? Parse(string line)
    {
        var match = _regex.Match(line);

        var dateTimeGroup = match.Groups["datetime"];
        var messageGroup = match.Groups["message"];
        var levelGroup = match.Groups["level"];

        if (!match.Success
            || !dateTimeGroup.Success
            || !messageGroup.Success)
        {
            return null;
        }

        ParsedFieldValue[] fields = [];
        if (match.Groups.Count - FIXED_GROUPS_COUNT > 0)
        {
            int j = 0;
            fields = new ParsedFieldValue[match.Groups.Count - FIXED_GROUPS_COUNT];
            for (var i = 1; i < match.Groups.Count; i++)
            {
                var group = match.Groups[i];
                if (group == dateTimeGroup || group == messageGroup || group == levelGroup)
                    continue;
                fields[j] = new ParsedFieldValue(group.Name, group.Value);
                j++;
            }
        }

        return new ParsedLine(
            dateTimeGroup.Value,
            levelGroup.Success ? levelGroup.Value : DEFAULT_LEVEL,
            fields,
            messageGroup.Value);
    }
}
