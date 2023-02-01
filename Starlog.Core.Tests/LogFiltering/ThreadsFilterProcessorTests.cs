using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Tests.LogFiltering;

public sealed class ThreadsFilterProcessorTests
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();

    [Theory]
    [InlineData(false, "1|2|3", "2", true)]
    [InlineData(false, "1|2|3", "4", false)]
    [InlineData(true, "1|2|3", "2", false)]
    [InlineData(true, "1|2|3", "4", true)]
    public void IsMatch_Scenarios(bool exclude, string threadFilters, string thread, bool expected)
    {
        // Arrange
        var sut = new ThreadsFilterProcessor();
        var profileFilter = new ThreadsProfileFilter(_fixture.Create<LogFilter>())
        {
            Threads = threadFilters.Split('|'),
            Exclude = exclude
        };
        var logRecord = _fixture.Build<LogRecord>()
            .With(x => x.Thread, thread)
            .Create();

        // Act
        var actual = sut.IsMatch(profileFilter, logRecord);

        // Verify
        Assert.Equal(expected, actual);
    }
}
