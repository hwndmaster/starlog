using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogReading.PlainTextLogCodecParsers;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core.Tests.LogReading.PlainTextLogCodecParsers;

public sealed class PlainTextLogCodecLineMaskPatternParserTests
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly TestLogger<PlainTextLogCodecLineMaskPatternParser> _logger = new();

    [Fact]
    public void Parse_HappyFlowScenario()
    {
        // Arrange
        const string pattern = "%{datetime} %{level} %{thread} %{logger} - %{message}";
        const string dateTimeFormat = "dd-MM-yy HH:mm:ss.fff";
        const string line = "12-34-56 11:22:33.444 INFO 888 Component1 - Some Message with 123 numbers, 50% percents + $[y]mb\\ols!";
        var sut = CreateSystemUnderTest(dateTimeFormat, pattern);

        // Act
        var result = sut.Parse(line);

        // Verify
        Assert.NotNull(result);
        Assert.Equal("12-34-56 11:22:33.444", result.Value.DateTime);
        Assert.Equal("INFO", result.Value.Level);
        Assert.Equal("thread", result.Value.Fields[0].FieldName);
        Assert.Equal("888", result.Value.Fields[0].Value);
        Assert.Equal("logger", result.Value.Fields[1].FieldName);
        Assert.Equal("Component1", result.Value.Fields[1].Value);
        Assert.Equal("Some Message with 123 numbers, 50% percents + $[y]mb\\ols!", result.Value.Message);
    }

    [Fact]
    public void Parse_WhenInvalidGroup_ReturnsNull()
    {
        // Arrange
        const string pattern = @"%{datetime %{level} %{thread} %{logger} - %{message}";
        const string dateTimeFormat = "dd-MM-yy HH:mm:ss.fff";
        var sut = CreateSystemUnderTest(dateTimeFormat, pattern);

        // Act
        var result = sut.Parse(_fixture.Create<string>());

        // Verify
        Assert.Null(result);
        Assert.Equal(LogLevel.Warning, _logger.Logs.Single().LogLevel);
    }

    [Fact]
    public void Parse_WhenInvalidGroupAtTheEndOfLine_ReturnsNull()
    {
        // Arrange
        const string pattern = @"%{datetime} %{level} %{thread} %{logger} - %{message";
        const string dateTimeFormat = "dd-MM-yy HH:mm:ss.fff";
        var sut = CreateSystemUnderTest(dateTimeFormat, pattern);

        // Act
        var result = sut.Parse(_fixture.Create<string>());

        // Verify
        Assert.Null(result);
        Assert.Equal(LogLevel.Warning, _logger.Logs.Single().LogLevel);
    }

    private PlainTextLogCodecLineMaskPatternParser CreateSystemUnderTest(string dateTimeFormat, string pattern)
    {
        return new PlainTextLogCodecLineMaskPatternParser(dateTimeFormat, pattern, new MaskPatternParser(new TestLogger<MaskPatternParser>()), _logger);
    }
}
