using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Tests.LogFiltering;

public sealed class LoggersFilterProcessorTests
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();

    [Fact]
    public void IsMatch_WhenNoExclude_HappyFlowScenario()
    {
        // Arrange
        var sut = new LoggersFilterProcessor();
        var profileFilter = new LoggersProfileFilter(_fixture.Create<LogFilter>())
        {
            LoggerNames = _fixture.CreateMany<string>().ToArray(),
            Exclude = false
        };
        var logRecord = _fixture.Build<LogRecord>()
            .With(x => x.Logger, new LoggerRecord(_fixture.Create<int>(), profileFilter.LoggerNames[1]))
            .Create();

        // Act
        var actual = sut.IsMatch(profileFilter, logRecord);

        // Verify
        Assert.True(actual);
    }

    [Fact]
    public void IsMatch_WhenExclude_HappyFlowScenario()
    {
        // Arrange
        var sut = new LoggersFilterProcessor();
        var profileFilter = new LoggersProfileFilter(_fixture.Create<LogFilter>())
        {
            LoggerNames = _fixture.CreateMany<string>().ToArray(),
            Exclude = true
        };
        var logRecord = _fixture.Create<LogRecord>();

        // Act
        var actual = sut.IsMatch(profileFilter, logRecord);

        // Verify
        Assert.True(actual);
    }

    [Fact]
    public void IsMatch_WhenNotMatching_ReturnsFalse()
    {
        // Arrange
        var sut = new LoggersFilterProcessor();
        var profileFilter = new LoggersProfileFilter(_fixture.Create<LogFilter>())
        {
            LoggerNames = _fixture.CreateMany<string>().ToArray(),
        };
        var logRecord = _fixture.Create<LogRecord>();

        // Act
        var actual = sut.IsMatch(profileFilter, logRecord);

        // Verify
        Assert.False(actual);
    }

    [Fact]
    public void IsMatch_WhenExclude_AndMatching_ReturnsFalse()
    {
        // Arrange
        var sut = new LoggersFilterProcessor();
        var profileFilter = new LoggersProfileFilter(_fixture.Create<LogFilter>())
        {
            LoggerNames = _fixture.CreateMany<string>().ToArray(),
            Exclude = true
        };
        var logRecord = _fixture.Build<LogRecord>()
            .With(x => x.Logger, new LoggerRecord(_fixture.Create<int>(), profileFilter.LoggerNames[1]))
            .Create();

        // Act
        var actual = sut.IsMatch(profileFilter, logRecord);

        // Verify
        Assert.False(actual);
    }
}
