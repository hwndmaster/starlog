namespace Genius.Starlog.Core.LogReading.PlainTextLogCodecParsers;

// TODO: Cover with unit tests
internal class PlainTextLogCodecLineMaskPatternParser : IPlainTextLogCodecLineParser
{
    private readonly string _pattern;

    public PlainTextLogCodecLineMaskPatternParser(string pattern)
    {
        _pattern = pattern.NotNull();
    }

    public ParsedLine? Parse(string line)
    {
        throw new NotImplementedException();
    }
}
