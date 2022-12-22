using Genius.Starlog.Core.CommandHandlers;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;

namespace Genius.Starlog.Core.Tests.CommandHandlers;

public sealed class ProfileDeleteCommandHandlerTests
{
    private readonly ProfileHarness _harness = new();
    private readonly ProfileDeleteCommandHandler _sut;

    public ProfileDeleteCommandHandlerTests()
    {
        _sut = new(_harness.ProfileRepo, _harness.EventBus);
    }

    [Fact]
    public async Task Process_HappyFlowScenario()
    {
        // Arrange
        var command = _harness.Create<ProfileDeleteCommand>();

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        Mock.Get(_harness.ProfileRepo).Verify(x => x.DeleteAsync(command.ProfileId));
        _harness.VerifyEventPublished<ProfilesAffectedEvent>();
    }
}
