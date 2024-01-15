using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core.LogReading.PlainTextLogCodecParsers;

internal class PlainTextLogCodecLineMaskPatternParser : IPlainTextLogCodecLineParser
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
        string resultingPattern = string.Empty;
        for (var i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] == '%' && pattern.Length > i + 1 && pattern[i + 1] == '{')
            {
                var indexStart = i;
                while (i < pattern.Length && pattern[i] != '}')
                {
                    if (pattern[i] is ' ')
                    {
                        _logger.LogWarning("Mask Pattern contains non closing group: " + pattern);
                        return null;
                    }
                    i++;
                }

                if (i == pattern.Length)
                {
                    _logger.LogWarning("Mask Pattern contains non closing group: " + pattern);
                    return null;
                }

                var groupName = pattern.Substring(indexStart + 2, i - indexStart - 2);
                resultingPattern += $"(?<{groupName}>";
                if (groupName is "datetime")
                {
                    resultingPattern += dateTimePattern;
                }
                else if (groupName is "message")
                {
                    resultingPattern += ".+";
                }
                else
                {
                    resultingPattern += @"\w+";
                }
                resultingPattern += ")";
            }
            else if (pattern[i] == ' ')
            {
                resultingPattern += @"\s";
            }
            else
            {
                resultingPattern += pattern[i];
            }
        }

        try
        {
            new Regex(resultingPattern);
        }
        catch (Exception)
        {
            _logger.LogWarning("Couldn't create regex object for Mask Pattern: " + pattern);
            return null;
        }

        return resultingPattern;
    }
}
