using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.TestingUtil;

public sealed class FilterHarness
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture(useMutableValueTypeGenerator: true);
    private readonly ILogFilterContainer _logFilterContainerMock = A.Fake<ILogFilterContainer>();

    private readonly List<(ProfileFilterBase, LogRecord)> _matchingCheckedFor = new();

    public IFilterProcessor SetupFilterProcessor(ProfileFilterBase filter, LogRecord? matchingRecord = null)
    {
        var filterProcessorMock = A.Fake<IFilterProcessor>();
        A.CallTo(() => _logFilterContainerMock.GetFilterProcessor(filter)).Returns(filterProcessorMock);
        if (matchingRecord is not null)
        {
            A.CallTo(() => filterProcessorMock.IsMatch(filter, matchingRecord.Value))
                .Invokes(() => _matchingCheckedFor.Add((filter, matchingRecord.Value)))
                .Returns(true);
        }
        return filterProcessorMock;
    }

    public IFilterProcessor SetupFilterProcessor(ProfileFilterBase filter, LogRecord matchingRecord, Func<ProfileFilterBase, bool> matchHandler)
    {
        var filterProcessorMock = A.Fake<IFilterProcessor>();
        A.CallTo(() => _logFilterContainerMock.GetFilterProcessor(filter)).Returns(filterProcessorMock);

        A.CallTo(() => filterProcessorMock.IsMatch(filter, matchingRecord))
            .Invokes(() => _matchingCheckedFor.Add((filter, matchingRecord)))
            .ReturnsLazily(() => matchHandler(filter));

        return filterProcessorMock;
    }

    public IFixture Fixture => _fixture;
    public IEnumerable<(ProfileFilterBase, LogRecord)> MatchingCheckedFor => _matchingCheckedFor;
    public ILogFilterContainer LogFilterContainer => _logFilterContainerMock;
}
