using Genius.Starlog.Core.LogReading.PlainTextLogCodecParsers;

namespace Genius.Starlog.Core.Tests.LogReading.PlainTextLogCodecParsers;

public sealed class PlainTextLogCodecLineRegexParserTests
{
    [Fact]
    public void Parse_HappyFlowScenario()
    {
        // Arrange
        var pattern = @"(?<datetime>%datetime%)\s(?<level>%level%)\s(?<thread>%thread%)\s(?<logger>%logger%)\s(?<message>%msg%)";
        var line = "%datetime% %level% %thread% %logger% %msg%";
        var sut = new PlainTextLogCodecLineRegexParser(pattern);

        // Act
        var result = sut.Parse(line);

        // Verify
        Assert.NotNull(result);
        Assert.Equal("%datetime%", result.Value.DateTime);
        Assert.Equal("%level%", result.Value.Level);
        Assert.Equal("thread", result.Value.Fields[0].FieldName);
        Assert.Equal("%thread%", result.Value.Fields[0].Value);
        Assert.Equal("logger", result.Value.Fields[1].FieldName);
        Assert.Equal("%logger%", result.Value.Fields[1].Value);
        Assert.Equal("%msg%", result.Value.Message);
    }

    [Fact]
    public void Parse_DateTimeMissing_ReturnsNull()
    {
        // Arrange
        var pattern = @"(?<datetime>%datetime%)\s(?<level>%level%)\s(?<thread>%thread%)\s(?<logger>%logger%)\s(?<message>%msg%)";
        var line = "%datetime2% %level% %thread% %logger% %msg%";
        var sut = new PlainTextLogCodecLineRegexParser(pattern);

        // Act
        var result = sut.Parse(line);

        // Verify
        Assert.Null(result);
    }

    [Fact]
    public void Parse_MessageMissing_ReturnsNull()
    {
        // Arrange
        var pattern = @"(?<datetime>%datetime%)\s(?<level>%level%)\s(?<thread>%thread%)\s(?<logger>%logger%)\s(?<message>%msg%)";
        var line = "%datetime% %level% %thread% %logger% %msg2%";
        var sut = new PlainTextLogCodecLineRegexParser(pattern);

        // Act
        var result = sut.Parse(line);

        // Verify
        Assert.Null(result);
    }

    [Fact]
    public void Parse_LevelIsMissing_DefaultAreUsed()
    {
        // Arrange
        var pattern = @"(?<datetime>%datetime%)\s(?<message>%msg%)";
        var line = "%datetime% %msg%";
        var sut = new PlainTextLogCodecLineRegexParser(pattern);

        // Act
        var result = sut.Parse(line);

        // Verify
        Assert.NotNull(result);
        Assert.Equal("%datetime%", result.Value.DateTime);
        Assert.Equal("INFO", result.Value.Level);
        Assert.Empty(result.Value.Fields);
        Assert.Equal("%msg%", result.Value.Message);
    }
}
