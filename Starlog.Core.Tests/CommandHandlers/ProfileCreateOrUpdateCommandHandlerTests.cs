using Genius.Starlog.Core.CommandHandlers;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.TestingUtil;

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
        var command = _harness.Fixture.Create<ProfileCreateCommand>();

        // Act
        var guid = await _sut.ProcessAsync(command);

        // Verify
        Mock.Get(_harness.ProfileRepo).Verify(x => x.StoreAsync(It.Is<Profile>(x =>
            x.Name == command.Name
            && x.Path == command.Path
            && x.Settings == command.Settings)));
        _harness.VerifyEventPublished<ProfilesAffectedEvent>();
    }

    [Fact]
    public async Task ProcessProfileUpdateCommand_HappyFlowScenario()
    {
        // Arrange
        var profile = _harness.CreateProfile();
        var command = new ProfileUpdateCommand(profile.Id)
        {
            Name = _harness.Fixture.Create<string>(),
            Path = _harness.Fixture.Create<string>(),
            Settings = _harness.Fixture.Create<ProfileSettings>()
        };

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        Mock.Get(_harness.ProfileRepo).Verify(x => x.StoreAsync(It.Is<Profile>(x =>
            x.Id == command.ProfileId
            && x.Name == command.Name
            && x.Path == command.Path
            && x.Settings == command.Settings)));
        _harness.VerifyEventPublished<ProfilesAffectedEvent>();
    }

    [Fact]
    public async Task ProcessProfileUpdateCommand_WhenNoProfileFound_ThrowsException()
    {
        // Arrange
        var command = new ProfileUpdateCommand(_harness.Fixture.Create<Guid>())
        {
            Name = _harness.Fixture.Create<string>(),
            Path = _harness.Fixture.Create<string>(),
            Settings = _harness.Fixture.Create<ProfileSettings>()
        };

        // Act
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _sut.ProcessAsync(command));
    }
}
