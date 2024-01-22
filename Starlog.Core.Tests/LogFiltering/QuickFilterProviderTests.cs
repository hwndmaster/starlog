using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.Configuration;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Microsoft.Extensions.Options;

namespace Genius.Starlog.Core.Tests.LogFiltering;

public sealed class QuickFilterProviderTests
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly LogFilterContainer _logFilterContainer;
    private readonly QuickFilterProvider _sut;

    public QuickFilterProviderTests()
    {
        _logFilterContainer = new LogFilterContainer(Array.Empty<IFilterProcessor>());

        _logFilterContainer.RegisterLogFilter<LogLevelsProfileFilter, TestFilterProcessor>(_fixture.Create<LogFilter>());
        _logFilterContainer.RegisterLogFilter<MessageProfileFilter, TestFilterProcessor>(_fixture.Create<LogFilter>());
        _logFilterContainer.RegisterLogFilter<ThreadsProfileFilter, TestFilterProcessor>(_fixture.Create<LogFilter>());

        var logLevelMappingConfig = Mock.Of<IOptions<LogLevelMappingConfiguration>>(x => x.Value == new LogLevelMappingConfiguration
        {
            TreatAsMinor = _fixture.CreateMany<string>().ToArray(),
            TreatAsWarning = _fixture.CreateMany<string>().ToArray(),
            TreatAsError = _fixture.CreateMany<string>().ToArray(),
            TreatAsCritical = _fixture.CreateMany<string>().ToArray(),
        });

        _sut = new QuickFilterProvider(_logFilterContainer, logLevelMappingConfig);
    }

    [Fact]
    public void GetQuickFilters_IdentifiersArePreserved()
    {
        // Arrange

        // Act
        var actual1 = _sut.GetQuickFilters().ToList();
        var actual2 = _sut.GetQuickFilters().ToList();

        // Verify
        Assert.Equal(5, actual1.Count);
        Assert.Equal(actual1.Select(x => x.Id), actual2.Select(x => x.Id));
    }

    private sealed class TestFilterProcessor : IFilterProcessor
    {
        public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log)
        {
            throw new NotImplementedException();
        }
    }
}
