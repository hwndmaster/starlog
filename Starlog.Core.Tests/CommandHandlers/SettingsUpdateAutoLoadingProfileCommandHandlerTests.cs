using Genius.Starlog.Core.CommandHandlers;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.Tests.CommandHandlers;

public sealed class SettingsUpdateAutoLoadingProfileCommandHandlerTests
{
    private readonly Mock<ISettingsRepository> _repoMock = new();
    private readonly SettingsUpdateAutoLoadingProfileCommandHandler _sut;

    public SettingsUpdateAutoLoadingProfileCommandHandlerTests()
    {
        _sut = new(_repoMock.Object);
    }

    [Fact]
    public async Task Process_HappyFlowScenario()
    {
        // Arrange
        var command = new SettingsUpdateAutoLoadingProfileCommand(Guid.NewGuid());
        var settings = new Settings
        {
            AutoLoadPreviouslyOpenedProfile = true
        };
        _repoMock.Setup(x => x.Get()).Returns(settings);

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        _repoMock.Verify(x => x.Store(settings));
        Assert.Equal(command.ProfileId, settings.AutoLoadProfile);
    }

    [Fact]
    public async Task Process_WhenAutoLoadPreviouslyOpenedProfileDisabled_DoesNothing()
    {
        // Arrange
        var command = new SettingsUpdateAutoLoadingProfileCommand(Guid.NewGuid());
        var settings = new Settings
        {
            AutoLoadPreviouslyOpenedProfile = false
        };
        Assert.Null(settings.AutoLoadProfile);
        _repoMock.Setup(x => x.Get()).Returns(settings);

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        _repoMock.Verify(x => x.Store(settings), Times.Never);
        Assert.Null(settings.AutoLoadProfile);
    }
}
