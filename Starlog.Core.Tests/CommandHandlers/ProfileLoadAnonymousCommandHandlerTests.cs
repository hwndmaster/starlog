using Genius.Starlog.Core.CommandHandlers;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.TestingUtil;

namespace Genius.Starlog.Core.Tests.CommandHandlers;

public sealed class ProfileLoadAnonymousCommandHandlerTests
{
    private readonly ProfileHarness _harness = new();
    private readonly ProfileLoadAnonymousCommandHandler _sut;

    public ProfileLoadAnonymousCommandHandlerTests()
    {
        _sut = new(_harness.ProfileRepo);
    }

    [Fact]
    public async Task Process_HappyFlowScenario()
    {
        // Arrange
        var command = _harness.Fixture.Create<ProfileLoadAnonymousCommand>();

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        A.CallTo(() => _harness.ProfileRepo.SetAnonymous(A<Profile>.That.Matches(x =>
            x.Id == Profile.AnonymousProfileId
            && x.Name == "Unnamed"
            && x.Settings == command.Settings))).MustHaveHappened();
    }
}
