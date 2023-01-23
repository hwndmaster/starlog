using Genius.Atom.Infrastructure;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFiltering;

namespace Genius.Starlog.Core.Tests.LogFiltering;

// TODO: Implement tests

public sealed class TimeAgoFilterProcessorTests
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly Mock<IDateTime> _dateTimeMock = new();

    [Fact]
    public void IsMatch_()
    {
        // Arrange
        var sut = new TimeAgoFilterProcessor(_dateTimeMock.Object);

        // Act
        // TODO: ...

        // Verify
        Assert.Fail("TBD");
    }
}
