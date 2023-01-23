using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.Core.Tests.LogFiltering;

// TODO: Implement tests

public sealed class LogRecordMatcherTests
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly Mock<ILogFilterContainer> _logFilterContainerMock = new();

    [Fact]
    public void IsMatch_HappyFlowScenario()
    {
        // Arrange
        var sut = new LogRecordMatcher(_logFilterContainerMock.Object);
        var context = _fixture.Create<LogRecordMatcherContext>();
        var logRecord = _fixture.Create<LogRecord>();

        // Act
        var actual = sut.IsMatch(context, logRecord);

        // Verify
        Assert.Fail("TBD");
    }
}
