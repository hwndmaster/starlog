using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core.LogReading.PlainTextLogCodecParsers;

internal sealed class PlainTextLogCodecLineMaskPatternParser : IPlainTextLogCodecLineParser
{
    private readonly ILogger<PlainTextLogCodecLineMaskPatternParser> _logger;
    private readonly PlainTextLogCodecLineRegexParser? _regexParser;

    public PlainTextLogCodecLineMaskPatternParser(string dateTimeFormat, string pattern, IMaskPatternParser maskPatternParser, ILogger<PlainTextLogCodecLineMaskPatternParser> logger)
    {
        Guard.NotNull(dateTimeFormat);
        Guard.NotNull(maskPatternParser);
        Guard.NotNull(pattern);

        _logger = logger.NotNull();

        var dateTimePattern = Regex.Replace(dateTimeFormat, @"\w", (Match _) => @"\d")
            .Replace(" ", @"\s");
        var resultingPattern = maskPatternParser.ConvertMaskPatternToRegexPattern(pattern, (groupName) =>
        {
            if (groupName is "datetime")
                return dateTimePattern;
            else if (groupName is "message")
                return ".+";
            return @"\w+";
        });

        if (resultingPattern is not null)
        {
            _regexParser = new PlainTextLogCodecLineRegexParser(resultingPattern);
        }
        else
        {
            _logger.LogWarning("Couldn't create regex object for Mask Pattern: {Pattern}", pattern);
        }
    }

    public ParsedLine? Parse(string line)
    {
        return _regexParser?.Parse(line);
    }
}
