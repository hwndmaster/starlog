using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Tests.LogFiltering;

public sealed class LogLevelsFilterProcessorTests
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();

    [Fact]
    public void IsMatch_WhenNoExclude_HappyFlowScenario()
    {
        // Arrange
        var sut = new LogLevelsFilterProcessor();
        var profileFilter = new LogLevelsProfileFilter(_fixture.Create<LogFilter>())
        {
            LogLevels = _fixture.CreateMany<string>().ToArray(),
            Exclude = false
        };
        var logRecord = _fixture.Build<LogRecord>()
            .With(x => x.Level, new LogLevelRecord(_fixture.Create<int>(), profileFilter.LogLevels[1]))
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
        var sut = new LogLevelsFilterProcessor();
        var profileFilter = new LogLevelsProfileFilter(_fixture.Create<LogFilter>())
        {
            LogLevels = _fixture.CreateMany<string>().ToArray(),
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
        var sut = new LogLevelsFilterProcessor();
        var profileFilter = new LogLevelsProfileFilter(_fixture.Create<LogFilter>())
        {
            LogLevels = _fixture.CreateMany<string>().ToArray(),
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
        var sut = new LogLevelsFilterProcessor();
        var profileFilter = new LogLevelsProfileFilter(_fixture.Create<LogFilter>())
        {
            LogLevels = _fixture.CreateMany<string>().ToArray(),
            Exclude = true
        };
        var logRecord = _fixture.Build<LogRecord>()
            .With(x => x.Level, new LogLevelRecord(_fixture.Create<int>(), profileFilter.LogLevels[1]))
            .Create();

        // Act
        var actual = sut.IsMatch(profileFilter, logRecord);

        // Verify
        Assert.False(actual);
    }
}
