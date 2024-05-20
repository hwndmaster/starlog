using Genius.Starlog.Core.CommandHandlers;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.TestingUtil;

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
        A.CallTo(() => _harness.ProfileRepo.StoreAsync(profile)).MustHaveHappened();
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
        A.CallTo(() => _harness.ProfileRepo.StoreAsync(profile)).MustNotHaveHappened();
        _harness.VerifyEventPublished<ProfilesAffectedEvent>(numberOfTimes: 0);
        Assert.Equal(messageParsingCount, profile.MessageParsings.Count);
    }
}
