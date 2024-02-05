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
        var pattern = @"%{datetime} %{level} %{thread} %{logger} - %{message}";
        var dateTimeFormat = "dd-MM-yy HH:mm:ss.fff";
        var line = "12-34-56 11:22:33.444 INFO 888 Component1 - Some Message with 123 numbers, 50% percents + $[y]mb\\ols!";
        var sut = new PlainTextLogCodecLineMaskPatternParser(dateTimeFormat, pattern, _logger);

        // Act
        var result = sut.Parse(line);

        // Verify
        Assert.NotNull(result);
        Assert.Equal("12-34-56 11:22:33.444", result.Value.DateTime);
        Assert.Equal("INFO", result.Value.Level);
        Assert.Equal("888", result.Value.Thread);
        Assert.Equal("Component1", result.Value.Logger);
        Assert.Equal("Some Message with 123 numbers, 50% percents + $[y]mb\\ols!", result.Value.Message);
    }

    [Fact]
    public void Parse_WhenInvalidGroup_ReturnsNull()
    {
        // Arrange
        var pattern = @"%{datetime %{level} %{thread} %{logger} - %{message}";
        var dateTimeFormat = "dd-MM-yy HH:mm:ss.fff";
        var sut = new PlainTextLogCodecLineMaskPatternParser(dateTimeFormat, pattern, _logger);

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
        var pattern = @"%{datetime} %{level} %{thread} %{logger} - %{message";
        var dateTimeFormat = "dd-MM-yy HH:mm:ss.fff";
        var sut = new PlainTextLogCodecLineMaskPatternParser(dateTimeFormat, pattern, _logger);

        // Act
        var result = sut.Parse(_fixture.Create<string>());

        // Verify
        Assert.Null(result);
        Assert.Equal(LogLevel.Warning, _logger.Logs.Single().LogLevel);
    }
}
