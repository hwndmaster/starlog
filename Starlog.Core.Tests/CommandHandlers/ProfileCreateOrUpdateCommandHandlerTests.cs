using Genius.Starlog.Core.CommandHandlers;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Tests.CommandHandlers;

public sealed class ProfileCreateOrUpdateCommandHandlerTests
{
    private readonly ProfileHarness _harness = new();
    private readonly ProfileCreateOrUpdateCommandHandler _sut;

    public ProfileCreateOrUpdateCommandHandlerTests()
    {
        _sut = new(_harness.ProfileRepo, _harness.ProfileQuery, _harness.EventBus);
    }

    [Fact]
    public async Task ProcessProfileCreateCommand_HappyFlowScenario()
    {
        // Arrange
        var command = _harness.Create<ProfileCreateCommand>();

        // Act
        var guid = await _sut.ProcessAsync(command);

        // Verify
        Mock.Get(_harness.ProfileRepo).Verify(x => x.StoreAsync(It.Is<Profile>(x =>
            x.Name == command.Name
            && x.Path == command.Path
            && x.LogReader == command.LogReader
            && x.FileArtifactLinesCount == command.FileArtifactLinesCount)));
        _harness.VerifyEventPublished<ProfilesAffectedEvent>();
    }

    [Fact]
    public async Task ProcessProfileUpdateCommand_HappyFlowScenario()
    {
        // Arrange
        var profile = _harness.CreateProfile();
        var command = new ProfileUpdateCommand(profile.Id)
        {
            Name = _harness.Create<string>(),
            Path = _harness.Create<string>(),
            LogReader = _harness.Create<ProfileLogReadBase>(),
            FileArtifactLinesCount = _harness.Create<int>(),
        };

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        Mock.Get(_harness.ProfileRepo).Verify(x => x.StoreAsync(It.Is<Profile>(x =>
            x.Id == command.ProfileId
            && x.Name == command.Name
            && x.Path == command.Path
            && x.LogReader == command.LogReader
            && x.FileArtifactLinesCount == command.FileArtifactLinesCount)));
        _harness.VerifyEventPublished<ProfilesAffectedEvent>();
    }

    [Fact]
    public async Task ProcessProfileUpdateCommand_WhenNoProfileFound_ThrowsException()
    {
        // Arrange
        var command = new ProfileUpdateCommand(_harness.Create<Guid>())
        {
            Name = _harness.Create<string>(),
            Path = _harness.Create<string>(),
            LogReader = _harness.Create<ProfileLogReadBase>(),
            FileArtifactLinesCount = _harness.Create<int>(),
        };

        // Act
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _sut.ProcessAsync(command));
    }
}
