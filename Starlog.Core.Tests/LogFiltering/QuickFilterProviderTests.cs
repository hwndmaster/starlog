using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Tests.LogFiltering;

public sealed class QuickFilterProviderTests
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly Mock<ILogFilterContainer> _logFilterContainerMock = new();
    private readonly QuickFilterProvider _sut;

    public QuickFilterProviderTests()
    {
        _sut = new QuickFilterProvider(_logFilterContainerMock.Object);
    }

    [Fact]
    public void IsMatch_WhenNoExclude_HappyFlowScenario()
    {
        // Arrange
        _logFilterContainerMock.Setup(x => x.CreateProfileFilter<LogLevelsProfileFilter>(It.IsAny<string>())).Returns(() =>
            _fixture.Create<LogLevelsProfileFilter>());
        _logFilterContainerMock.Setup(x => x.CreateProfileFilter<MessageProfileFilter>(It.IsAny<string>())).Returns(() =>
            _fixture.Create<MessageProfileFilter>());
        _logFilterContainerMock.Setup(x => x.CreateProfileFilter<ThreadsProfileFilter>(It.IsAny<string>())).Returns(() =>
            _fixture.Create<ThreadsProfileFilter>());

        // Act
        var actual = _sut.GetQuickFilters().ToList();

        // Verify
        Assert.Equal(5, actual.Count);
    }
}
