using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core;

public interface IMaskPatternParser
{
    string? ConvertMaskPatternToRegexPattern(string pattern, Func<string, string?> customFieldHandler);
}

internal sealed class MaskPatternParser : IMaskPatternParser
{
    private readonly ILogger<MaskPatternParser> _logger;

    public MaskPatternParser(ILogger<MaskPatternParser> logger)
    {
        _logger = logger.NotNull();
    }

    public string? ConvertMaskPatternToRegexPattern(string pattern, Func<string, string?> customFieldHandler)
    {
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

                var fieldCustomPattern = customFieldHandler(groupName);
                if (fieldCustomPattern is not null)
                {
                    resultingPattern.Append(fieldCustomPattern);
                }
                else
                {
                    resultingPattern.Append(".+");
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
