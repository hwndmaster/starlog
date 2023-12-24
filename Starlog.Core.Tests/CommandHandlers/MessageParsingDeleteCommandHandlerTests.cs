using Genius.Starlog.Core.CommandHandlers;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;

namespace Genius.Starlog.Core.Tests.CommandHandlers;

public sealed class MessageParsingDeleteCommandHandlerTests
{
    private readonly ProfileHarness _harness = new();
    private readonly MessageParsingDeleteCommandHandler _sut;

    public MessageParsingDeleteCommandHandlerTests()
    {
        _sut = new(_harness.ProfileRepo, _harness.ProfileQuery, _harness.EventBus);
    }

    [Fact]
    public async Task Process_HappyFlowScenario()
    {
        // Arrange
        var profile = _harness.CreateProfile();
        var messageParsingCount = profile.MessageParsings.Count;
        var messageParsingToDelete = profile.MessageParsings[1];
        var command = new MessageParsingDeleteCommand(profile.Id, messageParsingToDelete.Id);

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        Mock.Get(_harness.ProfileRepo).Verify(x => x.StoreAsync(profile));
        _harness.VerifyEventPublished<ProfilesAffectedEvent>();
        Assert.Equal(messageParsingCount - 1, profile.MessageParsings.Count);
        Assert.DoesNotContain(messageParsingToDelete, profile.MessageParsings);
    }

    [Fact]
    public async Task Process_WhenMessageParsingNotFound_ThenNothingToHappen()
    {
        // Arrange
        var profile = _harness.CreateProfile();
        var messageParsingCount = profile.MessageParsings.Count;
        var command = new MessageParsingDeleteCommand(profile.Id, Guid.NewGuid());

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        Mock.Get(_harness.ProfileRepo).Verify(x => x.StoreAsync(profile), Times.Never);
        _harness.VerifyEventPublished<ProfilesAffectedEvent>(Times.Never());
        Assert.Equal(messageParsingCount, profile.MessageParsings.Count);
    }
}
