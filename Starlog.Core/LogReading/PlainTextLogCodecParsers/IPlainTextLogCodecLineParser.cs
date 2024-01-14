namespace Genius.Starlog.Core.LogReading.PlainTextLogCodecParsers;

internal interface IPlainTextLogCodecLineParser
{
    ParsedLine? Parse(string line);
}
