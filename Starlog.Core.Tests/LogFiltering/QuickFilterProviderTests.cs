using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.Configuration;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Microsoft.Extensions.Options;

namespace Genius.Starlog.Core.Tests.LogFiltering;

public sealed class QuickFilterProviderTests
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture(useMutableValueTypeGenerator: true);
    private readonly LogFilterContainer _logFilterContainer;
    private readonly QuickFilterProvider _sut;
    private readonly Mock<ILogFieldsContainer> _logFieldContainerMock = new();

    public QuickFilterProviderTests()
    {
        _logFilterContainer = new LogFilterContainer(Array.Empty<IFilterProcessor>());

        _logFilterContainer.RegisterLogFilter<LogLevelsProfileFilter, TestFilterProcessor>(_fixture.Create<LogFilter>());
        _logFilterContainer.RegisterLogFilter<MessageProfileFilter, TestFilterProcessor>(_fixture.Create<LogFilter>());
        _logFilterContainer.RegisterLogFilter<FieldProfileFilter, TestFilterProcessor>(_fixture.Create<LogFilter>());

        var logLevelMappingConfig = Mock.Of<IOptions<LogLevelMappingConfiguration>>(x => x.Value == new LogLevelMappingConfiguration
        {
            TreatAsMinor = _fixture.CreateMany<string>().ToArray(),
            TreatAsWarning = _fixture.CreateMany<string>().ToArray(),
            TreatAsError = _fixture.CreateMany<string>().ToArray(),
            TreatAsCritical = _fixture.CreateMany<string>().ToArray(),
        });

        var logContainer = Mock.Of<ILogContainer>(x => x.GetFields() == _logFieldContainerMock.Object);

        _sut = new QuickFilterProvider(logContainer, _logFilterContainer, logLevelMappingConfig);
    }

    [Fact]
    public void GetQuickFilters_IdentifiersArePreserved()
    {
        // Arrange
        _logFieldContainerMock.Setup(x => x.GetThreadFieldIfAny()).Returns((_fixture.Create<int>(), _fixture.Create<string>()));

        // Act
        var actual1 = _sut.GetQuickFilters().ToList();
        var actual2 = _sut.GetQuickFilters().ToList();

        // Verify
        Assert.Equal(5, actual1.Count);
        Assert.Equal(actual1.Select(x => x.Id), actual2.Select(x => x.Id));
    }

    [Fact]
    public void GetQuickFilters_ForThreads_FieldIsSupplied()
    {
        // Arrange
        var threadField = (_fixture.Create<int>(), _fixture.Create<string>());
        _logFieldContainerMock.Setup(x => x.GetThreadFieldIfAny()).Returns(threadField);

        // Act
        var actual = _sut.GetQuickFilters().ToArray()[^2..];

        // Verify
        Assert.True(actual[0] is FieldProfileFilter);
        Assert.True(actual[1] is FieldProfileFilter);
        Assert.Equal(threadField.Item1, ((FieldProfileFilter)actual[0]).FieldId);
        Assert.Equal(threadField.Item1, ((FieldProfileFilter)actual[1]).FieldId);
    }

    private sealed class TestFilterProcessor : IFilterProcessor
    {
        public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log)
        {
            throw new NotImplementedException();
        }
    }
}
