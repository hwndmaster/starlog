using Genius.Starlog.Core.LogFiltering;

namespace Genius.Starlog.Core.Tests.LogFiltering;

// TODO: Implement tests

public sealed class QuickFilterProviderTests
{
    private readonly Mock<ILogFilterContainer> _logFilterContainerMock = new();

    [Fact]
    public void IsMatch_WhenNoExclude_HappyFlowScenario()
    {
        // Arrange
        var sut = new QuickFilterProvider(_logFilterContainerMock.Object);

        // Act
        var actual = sut.GetQuickFilters().ToList();

        // Verify
        Assert.Fail("TBD");
    }
}
