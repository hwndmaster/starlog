using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Tests.LogFiltering;

public sealed class LogRecordMatcherTests
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly Mock<ILogFilterContainer> _logFilterContainerMock = new();

    [Fact]
    public void IsMatch_HappyFlowScenario()
    {
        // Arrange
        var sut = new LogRecordMatcher(_logFilterContainerMock.Object);
        var recordTime = _fixture.Create<DateTimeOffset>().ToUniversalTime();
        var filters = _fixture.CreateMany<ProfileFilterBase>().ToImmutableArray();
        var context = new LogRecordMatcherContext(
            new LogRecordFilterContext(true, _fixture.CreateMany<string>().ToHashSet(), filters, _fixture.Create<bool>(), false),
            new LogRecordSearchContext(true, true, _fixture.Create<string>(), null,
                recordTime - TimeSpan.FromMilliseconds(_fixture.Create<int>()),
                recordTime + TimeSpan.FromMilliseconds(_fixture.Create<int>())));
        var logRecord = _fixture.Build<LogRecord>()
            .With(x => x.File, new FileRecord(Path.Combine(_fixture.Create<string>(), context.Filter.FilesSelected.First()), 0))
            .With(x => x.DateTime, recordTime)
            .With(x => x.Message, _fixture.Create<string>() + context.Search.SearchText + _fixture.Create<string>())
            .Create();
        foreach (var filter in filters)
        {
            const bool isMatch = true; // All filter processors report successful matching
            _logFilterContainerMock.Setup(x => x.GetFilterProcessor(filter))
                .Returns(Mock.Of<IFilterProcessor>(x => x.IsMatch(filter, logRecord) == isMatch));
        }

        // Act
        var actual = sut.IsMatch(context, logRecord);

        // Verify
        Assert.True(actual);
    }

    [Fact]
    public void IsMatch_ForRegex_HappyFlowScenario()
    {
        // Arrange
        var sut = new LogRecordMatcher(_logFilterContainerMock.Object);
        var recordTime = _fixture.Create<DateTimeOffset>().ToUniversalTime();
        var filters = _fixture.CreateMany<ProfileFilterBase>().ToImmutableArray();
        var context = new LogRecordMatcherContext(
            new LogRecordFilterContext(true, _fixture.CreateMany<string>().ToHashSet(), filters, _fixture.Create<bool>(), false),
            new LogRecordSearchContext(true, true, string.Empty, new Regex("t[es]{2}t"),
                recordTime - TimeSpan.FromMilliseconds(_fixture.Create<int>()),
                recordTime + TimeSpan.FromMilliseconds(_fixture.Create<int>())));
        var logRecord = _fixture.Build<LogRecord>()
            .With(x => x.File, new FileRecord(Path.Combine(_fixture.Create<string>(), context.Filter.FilesSelected.First()), 0))
            .With(x => x.DateTime, recordTime)
            .With(x => x.Message, "message test with content")
            .Create();
        foreach (var filter in filters)
        {
            const bool isMatch = true; // All filter processors report successful matching
            _logFilterContainerMock.Setup(x => x.GetFilterProcessor(filter))
                .Returns(Mock.Of<IFilterProcessor>(x => x.IsMatch(filter, logRecord) == isMatch));
        }

        // Act
        var actual = sut.IsMatch(context, logRecord);

        // Verify
        Assert.True(actual);
    }

    /// <summary>
    ///   When context is null, it means no filter has been applied.
    /// </summary>
    [Fact]
    public void IsMatch_WhenContextIsNull_ReturnsTrue()
    {
        // Arrange
        var sut = new LogRecordMatcher(_logFilterContainerMock.Object);
        var logRecord = _fixture.Create<LogRecord>();

        // Act
        var actual = sut.IsMatch(null, logRecord);

        // Verify
        Assert.True(actual);
    }

    [Fact]
    public void IsMatch_WhenFileDoesntMatch_ReturnsFalse()
    {
        // Arrange
        var sut = new LogRecordMatcher(_logFilterContainerMock.Object);
        var context = new LogRecordMatcherContext(
            LogRecordFilterContext.CreateEmpty() with { FilesSelected = _fixture.CreateMany<string>().ToHashSet() },
            LogRecordSearchContext.CreateEmpty());
        var logRecord = _fixture.Build<LogRecord>()
            .With(x => x.File, new FileRecord(Path.Combine(_fixture.Create<string>(), _fixture.Create<Generator<string>>().First(g => !context.Filter.FilesSelected.Contains(g))), 0))
            .Create();

        // Act
        var actual = sut.IsMatch(context, logRecord);

        // Verify
        Assert.False(actual);
    }

    [Theory]
    [InlineData(-100, -50, false)]
    [InlineData(-50, 50, true)]
    [InlineData(50, 100, false)]
    public void IsMatch_WhenDateNotInRange_ReturnsFalse(int shiftFromRecordTimeFilterFrom, int shiftFromRecordTimeFilterTo, bool expected)
    {
        // Arrange
        var sut = new LogRecordMatcher(_logFilterContainerMock.Object);
        var recordTime = _fixture.Create<DateTimeOffset>().ToUniversalTime();
        var context = new LogRecordMatcherContext(
            LogRecordFilterContext.CreateEmpty(),
            LogRecordSearchContext.CreateEmpty() with {
                DateFrom = recordTime + TimeSpan.FromMilliseconds(shiftFromRecordTimeFilterFrom),
                DateTo = recordTime + TimeSpan.FromMilliseconds(shiftFromRecordTimeFilterTo)
            });
        var logRecord = _fixture.Build<LogRecord>()
            .With(x => x.DateTime, recordTime)
            .Create();

        // Act
        var actual = sut.IsMatch(context, logRecord);

        // Verify
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsMatch_WhenFilterDoesntMatch_ReturnsFalse()
    {
        // Arrange
        var sut = new LogRecordMatcher(_logFilterContainerMock.Object);
        var filters = _fixture.CreateMany<ProfileFilterBase>().ToImmutableArray();
        var context = new LogRecordMatcherContext(
            LogRecordFilterContext.CreateEmpty() with { FiltersSelected = filters },
            LogRecordSearchContext.CreateEmpty());
        var logRecord = _fixture.Build<LogRecord>()
            .With(x => x.File, new FileRecord(Path.Combine(_fixture.Create<string>(), _fixture.Create<Generator<string>>().First(g => !context.Filter.FilesSelected.Contains(g))), 0))
            .Create();
        foreach (var filter in filters)
        {
            var isMatch = filter != filters.Last(); // To fail on the last filter.
            _logFilterContainerMock.Setup(x => x.GetFilterProcessor(filter))
                .Returns(Mock.Of<IFilterProcessor>(x => x.IsMatch(filter, logRecord) == isMatch));
        }

        // Act
        var actual = sut.IsMatch(context, logRecord);

        // Verify
        Assert.False(actual);
    }

    [Fact]
    public void IsMatch_WhenMessageNotMatching_ReturnsFalse()
    {
        // Arrange
        var sut = new LogRecordMatcher(_logFilterContainerMock.Object);
        var context = new LogRecordMatcherContext(
            LogRecordFilterContext.CreateEmpty(),
            LogRecordSearchContext.CreateEmpty() with {
                MessageSearchIncluded = true,
                SearchText = _fixture.Create<string>()
            });
        var logRecord = _fixture.Build<LogRecord>()
            .With(x => x.Message, _fixture.Create<string>())
            .Create();

        // Act
        var actual = sut.IsMatch(context, logRecord);

        // Verify
        Assert.False(actual);
    }

    [Fact]
    public void IsMatch_WhenMessageNotMatching_ForRegex_ReturnsFalse()
    {
        // Arrange
        var sut = new LogRecordMatcher(_logFilterContainerMock.Object);
        var context = new LogRecordMatcherContext(
            LogRecordFilterContext.CreateEmpty(),
            LogRecordSearchContext.CreateEmpty() with {
                MessageSearchIncluded = true,
                MessageSearchRegex = new Regex("never.matching.pattern")
            });
        var logRecord = _fixture.Build<LogRecord>()
            .With(x => x.Message, _fixture.Create<string>())
            .Create();

        // Act
        var actual = sut.IsMatch(context, logRecord);

        // Verify
        Assert.False(actual);
    }
}
