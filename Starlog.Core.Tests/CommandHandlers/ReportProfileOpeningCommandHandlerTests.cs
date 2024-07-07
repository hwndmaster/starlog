using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Atom.Infrastructure.TestingUtil.Events;
using Genius.Starlog.Core.CommandHandlers;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.Core.TestingUtil;

namespace Genius.Starlog.Core.Tests.CommandHandlers;

public sealed class ReportProfileOpeningCommandHandlerTests
{
    private readonly TestDateTime _dateTime = new();
    private readonly TestEventBus _eventBus = new();
    private readonly IProfileQueryService _profileQueryMock = A.Fake<IProfileQueryService>();
    private readonly IProfileRepository _profileRepoMock = A.Fake<IProfileRepository>();
    private readonly ISettingsRepository _settingsRepoMock = A.Fake<ISettingsRepository>();
    private readonly ReportProfileOpeningCommandHandler _sut;

    public ReportProfileOpeningCommandHandlerTests()
    {
        _sut = new(_dateTime, _eventBus, _profileQueryMock, _profileRepoMock, _settingsRepoMock);
    }

    [Fact]
    public async Task Process_HappyFlowScenario()
    {
        // Arrange
        var profileHarness = new ProfileHarness();
        var command = new ReportProfileOpeningCommand(Guid.NewGuid());
        var profile = profileHarness.CreateProfile();
        profile.LastOpened = _dateTime.Now;
        Profile? actualProfile = null;
        var settings = new Settings
        {
            AutoLoadPreviouslyOpenedProfile = true
        };
        A.CallTo(() => _profileQueryMock.FindByIdAsync(command.ProfileId)).Returns(profile);
        A.CallTo(() => _profileRepoMock.StoreAsync(A<Profile[]>.Ignored)).Invokes((Profile[] profiles) => { actualProfile = profiles[0]; });
        A.CallTo(() => _settingsRepoMock.Get()).Returns(settings);
        var lastOpenedTimeExpected = _dateTime.Now.AddMinutes(10);
        _dateTime.SetClock(lastOpenedTimeExpected);

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        Assert.Equal(lastOpenedTimeExpected, actualProfile.LastOpened);
        A.CallTo(() => _settingsRepoMock.Store(settings)).MustHaveHappened();
        Assert.Equal(command.ProfileId, settings.AutoLoadProfile);
        _eventBus.AssertSingleEvent<ProfilesAffectedEvent>();
        var lastOpenedUpdatedEvent = _eventBus.GetSingleEvent<ProfileLastOpenedUpdatedEvent>();
        Assert.Equal(command.ProfileId, lastOpenedUpdatedEvent.ProfileId);
        Assert.Equal(lastOpenedTimeExpected, lastOpenedUpdatedEvent.LastOpened);
    }

    [Fact]
    public async Task Process_WhenAutoLoadPreviouslyOpenedProfileDisabled_DoesNothing()
    {
        // Arrange
        var command = new ReportProfileOpeningCommand(Guid.NewGuid());
        var settings = new Settings
        {
            AutoLoadPreviouslyOpenedProfile = false
        };
        Assert.Null(settings.AutoLoadProfile);
        A.CallTo(() => _settingsRepoMock.Get()).Returns(settings);

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        A.CallTo(() => _settingsRepoMock.Store(settings)).MustNotHaveHappened();
        Assert.Null(settings.AutoLoadProfile);
    }
}
