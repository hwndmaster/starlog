using Genius.Starlog.Core.CommandHandlers;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.Tests.CommandHandlers;

public sealed class SettingsUpdateAutoLoadingProfileCommandHandlerTests
{
    private readonly ISettingsRepository _repoMock = A.Fake<ISettingsRepository>();
    private readonly SettingsUpdateAutoLoadingProfileCommandHandler _sut;

    public SettingsUpdateAutoLoadingProfileCommandHandlerTests()
    {
        _sut = new(_repoMock);
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
        A.CallTo(() => _repoMock.Get()).Returns(settings);

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        A.CallTo(() => _repoMock.Store(settings)).MustHaveHappened();
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
        A.CallTo(() => _repoMock.Get()).Returns(settings);

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        A.CallTo(() => _repoMock.Store(settings)).MustNotHaveHappened();
        Assert.Null(settings.AutoLoadProfile);
    }
}
