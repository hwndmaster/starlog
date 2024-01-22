using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.TestingUtil;

public sealed class FilterHarness
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly Mock<ILogFilterContainer> _logFilterContainerMock = new();

    private readonly List<(ProfileFilterBase, LogRecord)> _matchingCheckedFor = new();

    public IFilterProcessor SetupFilterProcessor(ProfileFilterBase filter, LogRecord? matchingRecord = null)
    {
        var filterProcessorMock = new Mock<IFilterProcessor>();
        _logFilterContainerMock.Setup(x => x.GetFilterProcessor(filter)).Returns(filterProcessorMock.Object);
        if (matchingRecord is not null)
        {
            filterProcessorMock.Setup(x => x.IsMatch(filter, matchingRecord.Value))
                .Returns(true)
                .Callback(() => _matchingCheckedFor.Add((filter, matchingRecord.Value)));
        }
        return filterProcessorMock.Object;
    }

    public IFilterProcessor SetupFilterProcessor(ProfileFilterBase filter, LogRecord matchingRecord, Func<ProfileFilterBase, bool> matchHandler)
    {
        var filterProcessorMock = new Mock<IFilterProcessor>();
        _logFilterContainerMock.Setup(x => x.GetFilterProcessor(filter)).Returns(filterProcessorMock.Object);

        filterProcessorMock.Setup(x => x.IsMatch(filter, matchingRecord))
            .Returns(() => matchHandler(filter))
            .Callback(() => _matchingCheckedFor.Add((filter, matchingRecord)));

        return filterProcessorMock.Object;
    }

    public IFixture Fixture => _fixture;
    public IEnumerable<(ProfileFilterBase, LogRecord)> MatchingCheckedFor => _matchingCheckedFor;
    public ILogFilterContainer LogFilterContainer => _logFilterContainerMock.Object;
}
