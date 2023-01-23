using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFiltering;

namespace Genius.Starlog.Core.Tests.LogFiltering;

// TODO: Implement tests

public sealed class MessageFilterProcessorTests
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();

    [Fact]
    public void IsMatch_()
    {
        // Arrange
        var sut = new MessageFilterProcessor();

        // Act
        // TODO: ...

        // Verify
        Assert.Fail("TBD");
    }
}
