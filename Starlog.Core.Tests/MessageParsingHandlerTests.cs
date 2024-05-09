using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Atom.Infrastructure.TestingUtil.Events;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.TestingUtil;

namespace Genius.Starlog.Core.Tests;

public sealed class MessageParsingHandlerTests : IDisposable
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly TestEventBus _eventBus = new();
    private readonly ProfileHarness _profileHarness = new();
    private readonly FilterHarness _filterHarness = new();
    private readonly Mock<IQuickFilterProvider> _quickFilterProviderMock = new();
    private readonly MessageParsingHandler _sut;

    public MessageParsingHandlerTests()
    {
        _sut = new(_profileHarness.CurrentProfile, _eventBus,
            new MaskPatternParser(new TestLogger<MaskPatternParser>()),
            _filterHarness.LogFilterContainer,
            _quickFilterProviderMock.Object);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    [Fact]
    public void RetrieveColumns_GivenMethodRegex_HappyFlowScenario()
    {
        // Arrange
        var messageParsing = SampleMessageParsingWithMethodRegex();

        // Act
        var columns = _sut.RetrieveColumns(messageParsing);

        // Verify
        Assert.Equal(new [] { "Lorem", "Ipsum" }, columns);
    }

    [Fact]
    public void RetrieveColumns_GivenMethodRegex_WhenProfilesAffectedEvent_ThenCacheRefreshed()
    {
        // Arrange
        var messageParsing = SampleMessageParsingWithMethodRegex();

        // Trigger to cache:
        var columns = _sut.RetrieveColumns(messageParsing);
        Assert.Equal(new [] { "Lorem", "Ipsum" }, columns);

        messageParsing.Pattern = @"(?<Foo>\w+)-(?<Bar>\w+)-(?<Baz>\w+)";

        // Pre-check: cache not yet updated before `ProfilesAffectedEvent` is triggered.
        columns = _sut.RetrieveColumns(messageParsing);
        Assert.Equal(new [] { "Lorem", "Ipsum" }, columns);

        // Act
        _eventBus.Publish(new ProfilesAffectedEvent());

        // Verify
        columns = _sut.RetrieveColumns(messageParsing);
        Assert.Equal(new [] { "Foo", "Bar", "Baz" }, columns);
    }

    [Fact]
    public void ParseMessage_GivenMethodRegex_HappyFlowScenario()
    {
        // Arrange
        var messageParsing = SampleMessageParsingWithMethodRegex();
        var logRecord = new LogRecord() with { Message = "Foo-Bar" };

        // Act
        var result = _sut.ParseMessage(messageParsing, logRecord).ToArray();

        // Verify
        Assert.Equal(new [] { "Foo", "Bar" }, result);
    }

    [Fact]
    public void ParseMessage_GivenMethodRegex_WhenMatchesInLogArtifact_HappyFlowScenario()
    {
        // Arrange
        var messageParsing = SampleMessageParsingWithMethodRegex();
        var logRecord = new LogRecord() with { Message = "blablabla", LogArtifacts = "Baz-Foo" };

        // Act
        var result = _sut.ParseMessage(messageParsing, logRecord).ToArray();

        // Verify
        Assert.Equal(new [] { "Baz", "Foo" }, result);
    }

    [Fact]
    public void ParseMessage_GivenMethodRegex_AndFiltersFromProfile_WhenMatching()
    {
        // Arrange
        var profile = _profileHarness.CreateProfile(setAsCurrent: true);
        var messageParsing = SampleMessageParsingWithMethodRegex();
        messageParsing.Filters = [profile.Filters[2].Id];
        var logRecord = new LogRecord() with { Message = "Foo-Bar" };
        _filterHarness.SetupFilterProcessor(profile.Filters[2], logRecord);

        // Act
        var result = _sut.ParseMessage(messageParsing, logRecord).ToArray();

        // Verify
        Assert.Contains(_filterHarness.MatchingCheckedFor, x => x == (profile.Filters[2], logRecord));
        Assert.Equal(new [] { "Foo", "Bar" }, result);
    }

    [Fact]
    public void ParseMessage_GivenMethodRegex_AndFiltersFromQuickProvider_WhenMatching()
    {
        // Arrange
        var profile = _profileHarness.CreateProfile(setAsCurrent: true);
        var quickFilters = _profileHarness.Fixture.CreateMany<TestProfileFilter>().ToArray();
        _quickFilterProviderMock.Setup(x => x.GetQuickFilters()).Returns(quickFilters);
        var messageParsing = SampleMessageParsingWithMethodRegex();
        messageParsing.Filters = new [] { quickFilters[1].Id };
        var logRecord = new LogRecord() with { Message = "Foo-Bar" };
        _filterHarness.SetupFilterProcessor(quickFilters[1], logRecord);

        // Act
        var result = _sut.ParseMessage(messageParsing, logRecord).ToArray();

        // Verify
        Assert.Contains(_filterHarness.MatchingCheckedFor, x => x == (quickFilters[1], logRecord));
        Assert.Equal(new [] { "Foo", "Bar" }, result);
    }

    [Fact]
    public void ParseMessage_GivenMethodRegex_AndFiltersFromProfile_WhenNotMatching()
    {
        // Arrange
        var profile = _profileHarness.CreateProfile(setAsCurrent: true);
        var messageParsing = SampleMessageParsingWithMethodRegex();
        messageParsing.Filters = new [] { profile.Filters[2].Id };
        var logRecord = new LogRecord() with { Message = "Foo-Bar" };
        _filterHarness.SetupFilterProcessor(profile.Filters[2], matchingRecord: null);

        // Act
        var result = _sut.ParseMessage(messageParsing, logRecord).ToArray();

        // Verify
        Assert.Empty(_filterHarness.MatchingCheckedFor);
        Assert.Equal(new [] { string.Empty, string.Empty }, result);
    }

    [Fact]
    public void ParseMessage_GivenMethodRegex_WhenMessageNotMatched_ReturnsEmptyStringsForAllColumns()
    {
        // Arrange
        var messageParsing = SampleMessageParsingWithMethodRegex();
        var logRecord = new LogRecord() with { Message = "blablabla_without_hypens" };

        // Act
        var result = _sut.ParseMessage(messageParsing, logRecord).ToArray();

        // Verify
        Assert.Equal(new [] { string.Empty, string.Empty }, result);
    }

    [Fact]
    public void ParseMessage_GivenMethodMaskPattern_HappyFlowScenario()
    {
        // Arrange
        var messageParsing = new MessageParsing
        {
            Name = _fixture.Create<string>(),
            Method = PatternType.MaskPattern,
            Pattern = "File %{File} read %{Count} logs"
        };
        var logRecord = new LogRecord() with { Message = "File SampleFileName.log read 15 logs" };

        // Act
        var result = _sut.ParseMessage(messageParsing, logRecord).ToArray();

        // Verify
        Assert.Equal(new [] { "SampleFileName.log", "15" }, result);
    }

    private MessageParsing SampleMessageParsingWithMethodRegex()
    {
        return new MessageParsing
        {
            Name = _fixture.Create<string>(),
            Method = PatternType.RegularExpression,
            Pattern = @"(?<Lorem>\w+)-(?<Ipsum>\w+)"
        };
    }
}
