using Genius.Starlog.Core.CommandHandlers;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Models;

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
        var command = _harness.Create<ProfileLoadAnonymousCommand>();

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        Mock.Get(_harness.ProfileRepo).Verify(x => x.SetAnonymous(It.Is<Profile>(x =>
            x.Id == Profile.AnonymousProfileId
            && x.Name == "Unnamed"
            && x.Path == command.Path
            && x.LogCodec == command.LogCodec
            && x.FileArtifactLinesCount == command.FileArtifactLinesCount)));
    }
}
