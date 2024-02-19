using System.Collections.Immutable;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Atom.Infrastructure.TestingUtil.Commands;
using Genius.Atom.Infrastructure.TestingUtil.Events;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Controllers;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.UI.Tests.Controllers;

public sealed class ProfileLoadingControllerTests
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly TestCommandBus _commandBus = new();
    private readonly Mock<ICurrentProfile> _currentProfileMock = new();
    private readonly Mock<IDialogCoordinator> _dialogCoordinatorMock = new();
    private readonly TestEventBus _eventBus = new();
    private readonly Mock<IMainController> _mainControllerMock = new();
    private readonly Mock<IMainViewModel> _mainViewModelMock = new();
    private readonly Mock<IProfileSettingsViewModelFactory> _profileSettingsViewModelFactoryMock = new();
    private readonly Mock<ISettingsQueryService> _settingsQueryMock = new();
    private readonly TestLogger<ProfileLoadingController> _logger = new();

    private readonly ProfileLoadingController _sut;

    private readonly TaskCompletionSource _loadedTask = new();

    public ProfileLoadingControllerTests()
    {
        _mainControllerMock.SetupGet(x => x.Loaded).Returns(_loadedTask.Task);

        _sut = new(_commandBus,
            _currentProfileMock.Object,
            _dialogCoordinatorMock.Object,
            _eventBus,
            _logger,
            _mainControllerMock.Object,
            new Lazy<IMainViewModel>(() => _mainViewModelMock.Object),
            _settingsQueryMock.Object,
            _profileSettingsViewModelFactoryMock.Object);
    }

    [Fact]
    public async Task LoadProfileSettingsAsync_GivenProfileSettings_HappyFlowScenario()
    {
        // Arrange
        var profileSettings = _fixture.Create<ProfileSettingsBase>();
        _currentProfileMock.Setup(x => x.LoadProfileAsync(It.IsAny<Profile>()))
            .Callback(() =>
            {
                // Ensure the MainViewModel is set to busy
                _mainControllerMock.Verify(x => x.SetBusy(true), Times.Once);
                _mainControllerMock.Verify(x => x.SetBusy(false), Times.Never);
            });
        var tab = SetupDummyTabsAnd<ILogsViewModel>();

        // Act
        var loadPathTask = _sut.LoadProfileSettingsAsync(profileSettings);
        _loadedTask.SetResult();
        await loadPathTask;

        // Verify
        var actualProfile = _commandBus.ReceivedResults.Single() as Profile;
        Assert.NotNull(actualProfile);
        _commandBus.AssertNoCommandsButOfType<ProfileLoadAnonymousCommand>();
        _commandBus.AssertSingleCommand<ProfileLoadAnonymousCommand>(
            x => Assert.Equal(profileSettings, x.Settings)
        );
        _currentProfileMock.Verify(x => x.LoadProfileAsync(actualProfile), Times.Once);
        _mainControllerMock.Verify(x => x.ShowLogsTab(), Times.Once);
        _mainControllerMock.Verify(x => x.SetBusy(false), Times.Once);
        Assert.DoesNotContain(_logger.Logs, x => x.LogLevel == LogLevel.Warning);
    }

    [Fact]
    public async Task AutoLoadProfileAsync_HappyFlowScenario()
    {
        // Arrange
        var settings = new Settings
        {
            AutoLoadPreviouslyOpenedProfile = true,
            AutoLoadProfile = _fixture.Create<Guid>()
        };
        _settingsQueryMock.Setup(x => x.Get()).Returns(settings);
        var executed = false;
        var loadProfileCommand = new Mock<IActionCommand>();
        loadProfileCommand.Setup(x => x.Execute(null)).Callback(() => executed = true);
        var profile = Mock.Of<IProfileViewModel>(x => x.Id == settings.AutoLoadProfile && x.LoadProfileCommand == loadProfileCommand.Object);
        var allProfiles = new DelayedObservableCollection<IProfileViewModel>(_fixture.CreateMany<IProfileViewModel>().Append(profile));
        var profileTab = SetupDummyTabsAnd<IProfilesViewModel>();
        Mock.Get(profileTab).Setup(x => x.Profiles).Returns(allProfiles);
        _mainControllerMock.Setup(x => x.GetProfilesTab()).Returns(profileTab);

        // Act
        var task = _sut.AutoLoadProfileAsync();
        _loadedTask.SetResult();
        await task;

        // Verify
        Assert.True(executed);
    }

    [Fact]
    public async Task AutoLoadProfileAsync_WhenLoadPathPreviouslyInvoked_Stops()
    {
        // Arrange
        var settings = _fixture.Create<ProfileSettingsBase>();
        _loadedTask.SetResult();

        // Pre-Act
        await _sut.LoadProfileSettingsAsync(settings);

        // Act
        await _sut.AutoLoadProfileAsync();

        // Verify
        _settingsQueryMock.Verify(x => x.Get(), Times.Never);
    }

    private T SetupDummyTabsAnd<T>()
        where T: class, ITabViewModel
    {
        var tab = Mock.Of<T>();
        var allTabs = _fixture.CreateMany<ITabViewModel>().Append(tab).ToImmutableArray();
        _mainViewModelMock.Setup(x => x.Tabs).Returns(allTabs);
        return tab;
    }
}
