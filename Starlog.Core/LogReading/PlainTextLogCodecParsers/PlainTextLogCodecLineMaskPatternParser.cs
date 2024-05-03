using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core.LogReading.PlainTextLogCodecParsers;

internal sealed class PlainTextLogCodecLineMaskPatternParser : IPlainTextLogCodecLineParser
{
    private readonly ILogger<PlainTextLogCodecLineMaskPatternParser> _logger;
    private readonly PlainTextLogCodecLineRegexParser? _regexParser = null;

    public PlainTextLogCodecLineMaskPatternParser(string dateTimeFormat, string pattern, ILogger<PlainTextLogCodecLineMaskPatternParser> logger)
    {
        Guard.NotNull(pattern);
        Guard.NotNull(dateTimeFormat);

        _logger = logger.NotNull();

        var resultingPattern = ConvertToRegexPattern(dateTimeFormat, pattern);

        if (resultingPattern is not null)
            _regexParser = new PlainTextLogCodecLineRegexParser(resultingPattern);
    }

    public ParsedLine? Parse(string line)
    {
        return _regexParser?.Parse(line);
    }

    private string? ConvertToRegexPattern(string dateTimeFormat, string pattern)
    {
        var dateTimePattern = Regex.Replace(dateTimeFormat, @"\w", (Match m) => @"\d")
            .Replace(" ", @"\s");
        StringBuilder resultingPattern = new();
        for (var i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] == '%' && pattern.Length > i + 1 && pattern[i + 1] == '{')
            {
                var indexStart = i;
                while (i < pattern.Length && pattern[i] != '}')
                {
                    if (pattern[i] is ' ')
                    {
                        _logger.LogWarning("Mask Pattern contains non closing group: {Pattern}", pattern);
                        return null;
                    }
                    i++;
                }

                if (i == pattern.Length)
                {
                    _logger.LogWarning("Mask Pattern contains non closing group: {Pattern}", pattern);
                    return null;
                }

                var groupName = pattern.Substring(indexStart + 2, i - indexStart - 2);
                resultingPattern.Append($"(?<{groupName}>");
                if (groupName is "datetime")
                {
                    resultingPattern.Append(dateTimePattern);
                }
                else if (groupName is "message")
                {
                    resultingPattern.Append(".+");
                }
                else
                {
                    resultingPattern.Append(@"\w+");
                }
                resultingPattern.Append(")");
            }
            else if (pattern[i] == ' ')
            {
                resultingPattern.Append(@"\s");
            }
            else
            {
                resultingPattern.Append(pattern[i]);
            }
        }

        var resultingPatternString = resultingPattern.ToString();

        try
        {
            var _ = new Regex(resultingPatternString);
        }
        catch (Exception)
        {
            _logger.LogWarning("Couldn't create regex object for Mask Pattern: {Pattern}", pattern);
            return null;
        }

        return resultingPatternString;
    }
}
