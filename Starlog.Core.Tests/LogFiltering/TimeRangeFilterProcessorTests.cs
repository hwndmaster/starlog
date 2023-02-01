using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Tests.LogFiltering;

public sealed class TimeRangeFilterProcessorTests
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly TimeRangeFilterProcessor _sut;

    public TimeRangeFilterProcessorTests()
    {
        _sut = new TimeRangeFilterProcessor();
    }

    [Theory]
    [InlineData(-100, -50, false)]
    [InlineData(-50, 50, true)]
    [InlineData(50, 100, false)]
    public void IsMatch_Scenarios(int shiftFromRecordTimeFilterFrom, int shiftFromRecordTimeFilterTo, bool expected)
    {
        // Arrange
        var recordTime = _fixture.Create<DateTimeOffset>().ToUniversalTime();
        var profileFilter = new TimeRangeProfileFilter(_fixture.Create<LogFilter>())
        {
            TimeFrom = recordTime + TimeSpan.FromMilliseconds(shiftFromRecordTimeFilterFrom),
            TimeTo = recordTime + TimeSpan.FromMilliseconds(shiftFromRecordTimeFilterTo),
        };
        var logRecord = _fixture.Build<LogRecord>()
            .With(x => x.DateTime, recordTime)
            .Create();

        // Act
        var actual = _sut.IsMatch(profileFilter, logRecord);

        // Verify
        Assert.Equal(expected, actual);
    }
}
