using Genius.Atom.Infrastructure.TestingUtil.Events;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Tests;

public sealed class MessageParsingHandlerTests
{
    private readonly TestEventBus _eventBus = new();
    private readonly MessageParsingHandler _sut;

    public MessageParsingHandlerTests()
    {
        _sut = new(_eventBus);
    }

    [Fact]
    public void RetrieveColumns_GivenMethodRegex_HappyFlowScenario()
    {
        // Arrange
        var messageParsing = SampleMessageParsingWithMethodRegex();

        // Act
        var columns = _sut.RetrieveColumns(messageParsing);

        // Verify
        Assert.Equal(2, columns.Length);
        Assert.Equal("Lorem", columns[0]);
        Assert.Equal("Ipsum", columns[1]);
    }

    [Fact]
    public void RetrieveColumns_GivenMethodRegex_WhenProfilesAffectedEvent_ThenCacheRefreshed()
    {
        // Arrange
        var messageParsing = SampleMessageParsingWithMethodRegex();
        var columns = _sut.RetrieveColumns(messageParsing);
        messageParsing.Pattern = @"(?<Foo>\w+)-(?<Bar>\w+)-(?<Baz>\w+)";

        // Pre-check: cache not yet updated before `ProfilesAffectedEvent` is triggered.
        columns = _sut.RetrieveColumns(messageParsing);
        Assert.Equal(2, columns.Length);
        Assert.Equal("Lorem", columns[0]);
        Assert.Equal("Ipsum", columns[1]);

        // Act
        _eventBus.Publish(new ProfilesAffectedEvent());

        // Verify
        columns = _sut.RetrieveColumns(messageParsing);
        Assert.Equal(3, columns.Length);
        Assert.Equal("Foo", columns[0]);
        Assert.Equal("Bar", columns[1]);
        Assert.Equal("Baz", columns[2]);
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
        Assert.Equal(2, result.Length);
        Assert.Equal("Foo", result[0]);
        Assert.Equal("Bar", result[1]);
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
        Assert.Equal(2, result.Length);
        Assert.Equal("Baz", result[0]);
        Assert.Equal("Foo", result[1]);
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
        Assert.Equal(2, result.Length);
        Assert.Equal(string.Empty, result[0]);
        Assert.Equal(string.Empty, result[1]);
    }

    private MessageParsing SampleMessageParsingWithMethodRegex()
    {
        return new MessageParsing
        {
            Method = MessageParsingMethod.RegEx,
            Pattern = @"(?<Lorem>\w+)-(?<Ipsum>\w+)"
        };
    }
}
