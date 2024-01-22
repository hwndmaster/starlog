using Genius.Starlog.Core.CommandHandlers;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.TestingUtil;

namespace Genius.Starlog.Core.Tests.CommandHandlers;

public sealed class MessageParsingCreateOrUpdateCommandHandlerTests
{
    private readonly ProfileHarness _harness = new();
    private readonly MessageParsingCreateOrUpdateCommandHandler _sut;

    public MessageParsingCreateOrUpdateCommandHandlerTests()
    {
        _sut = new(_harness.ProfileRepo, _harness.ProfileQuery, _harness.EventBus);
    }

    [Fact]
    public async Task Process_WhenNewMessageParsingProvided_ThenAddedToProfile()
    {
        // Arrange
        var profile = _harness.CreateProfile();
        var messageParsingsCount = profile.MessageParsings.Count;
        var command = _harness.Fixture.Build<MessageParsingCreateOrUpdateCommand>()
            .With(x => x.ProfileId, profile.Id)
            .Create();

        // Act
        var result = await _sut.ProcessAsync(command);

        // Verify
        Mock.Get(_harness.ProfileRepo).Verify(x => x.StoreAsync(profile));
        _harness.VerifyEventPublished<ProfilesAffectedEvent>();
        Assert.Equal(messageParsingsCount + 1, profile.MessageParsings.Count);
        Assert.Equal(command.MessageParsing, profile.MessageParsings[^1]);
        Assert.Equal(command.MessageParsing.Id, result.MessageParsingAdded);
        Assert.Null(result.MessageParsingUpdated);
    }

    [Fact]
    public async Task Process_WhenExistingMessageParsingProvided_ThenUpdatedAndPersisted()
    {
        // Arrange
        var profile = _harness.CreateProfile();
        var messageParsingsCount = profile.MessageParsings.Count;
        var updatingMessageParsingsId = _harness.Fixture.Create<Guid>();
        profile.MessageParsings[0] = _harness.Fixture.Build<MessageParsing>()
            .With(x => x.Id, updatingMessageParsingsId)
            .Create();
        var command = new MessageParsingCreateOrUpdateCommand
        {
            ProfileId = profile.Id,
            MessageParsing = _harness.Fixture.Build<MessageParsing>()
                .With(x => x.Id, updatingMessageParsingsId)
                .Create()
        };

        // Act
        var result = await _sut.ProcessAsync(command);

        // Verify
        Mock.Get(_harness.ProfileRepo).Verify(x => x.StoreAsync(profile));
        _harness.VerifyEventPublished<ProfilesAffectedEvent>();
        Assert.Equal(messageParsingsCount, profile.MessageParsings.Count);
        Assert.Equal(command.MessageParsing, profile.MessageParsings[0]);
        Assert.Null(result.MessageParsingAdded);
        Assert.Equal(command.MessageParsing.Id, result.MessageParsingUpdated);
    }
}
