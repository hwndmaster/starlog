using System.Text.RegularExpressions;

namespace Genius.Starlog.Core.LogReading.PlainTextLogCodecParsers;

internal class PlainTextLogCodecLineRegexParser : IPlainTextLogCodecLineParser
{
    private const string DEFAULT_LEVEL = "INFO";

    private readonly Regex _regex;

    public PlainTextLogCodecLineRegexParser(string pattern)
    {
        _regex = new(pattern);
    }

    public ParsedLine? Parse(string line)
    {
        var match = _regex.Match(line);
        if (!match.Success
            || !match.Groups["datetime"].Success
            || !match.Groups["message"].Success)
        {
            return null;
        }

        return new ParsedLine(
            match.Groups["datetime"].Value,
            match.Groups["level"].Success ? match.Groups["level"].Value : DEFAULT_LEVEL,
            match.Groups["thread"].Success ? match.Groups["thread"].Value : string.Empty,
            match.Groups["logger"].Success ? match.Groups["logger"].Value : string.Empty,
            match.Groups["message"].Value);
    }
}
