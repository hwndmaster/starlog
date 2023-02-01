using Genius.Atom.Infrastructure;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Tests.LogFiltering;

public sealed class TimeAgoFilterProcessorTests
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly TestDateTime _dateTime = new();
    private readonly TimeAgoFilterProcessor _sut;

    public TimeAgoFilterProcessorTests()
    {
        _sut = new TimeAgoFilterProcessor(_dateTime);
    }

    [Theory]
    [InlineData(100, 50, true)]
    [InlineData(100, 150, false)]
    public void IsMatch_Scenarios(ulong filterMs, double subtractFromNowMs, bool expected)
    {
        // Arrange
        var profileFilter = new TimeAgoProfileFilter(_fixture.Create<LogFilter>())
        {
            MillisecondsAgo = filterMs
        };
        _dateTime.SetClock(_fixture.Create<DateTime>());
        var logRecord = _fixture.Build<LogRecord>()
            .With(x => x.DateTime, (_dateTime.NowOffsetUtc + _dateTime.NowOffset.Offset).AddMilliseconds(-subtractFromNowMs))
            .Create();

        // Act
        var actual = _sut.IsMatch(profileFilter, logRecord);

        // Verify
        Assert.Equal(expected, actual);
    }
}
