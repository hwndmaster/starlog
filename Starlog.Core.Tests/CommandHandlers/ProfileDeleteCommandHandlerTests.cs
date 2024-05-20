using Genius.Starlog.Core.CommandHandlers;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.TestingUtil;

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
        var command = _harness.Fixture.Create<ProfileDeleteCommand>();

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        A.CallTo(() => _harness.ProfileRepo.DeleteAsync(command.ProfileId)).MustHaveHappened();
        _harness.VerifyEventPublished<ProfilesAffectedEvent>();
    }
}
