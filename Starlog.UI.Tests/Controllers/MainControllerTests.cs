using System.Collections.Immutable;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Atom.Infrastructure.TestingUtil.Commands;
using Genius.Atom.Infrastructure.TestingUtil.Events;
using Genius.Atom.Infrastructure.TestingUtil.Io;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Console;
using Genius.Starlog.UI.Controllers;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.UI.Tests.Controllers;

public sealed class MainControllerTests
{
    private const int MAINVIEWMODEL_TABS_COUNT = 4;

    private readonly Mock<IClipboardHelper> _clipboardHelperMock = new();
    private readonly Mock<ICurrentProfile> _currentProfileMock = new();
    private readonly Mock<IDialogCoordinator> _dialogCoordinatorMock = new();
    private readonly TestEventBus _eventBus = new();
    private readonly Mock<ISettingsQueryService> _settingsQueryMock = new();
    private readonly Mock<IUserInteraction> _uiMock = new();
    private readonly Mock<IMainViewModel> _mainViewModelMock = new();
    private readonly TestCommandBus _commandBus = new();
    private readonly TestLogger<MainController> _logger = new();
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();

    private readonly MainController _sut;

    public MainControllerTests()
    {
        _sut = new(_clipboardHelperMock.Object,
            _commandBus,
            _currentProfileMock.Object,
            _dialogCoordinatorMock.Object,
            _eventBus,
            new TestFileService(),
            Mock.Of<ILogCodecContainer>(),
            _settingsQueryMock.Object,
            _uiMock.Object,
            new Lazy<IMainViewModel>(() => _mainViewModelMock.Object), _logger);
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
                _mainViewModelMock.VerifySet(x => x.IsBusy = true);
            });
        var tab = SetupDummyTabsAnd<ILogsViewModel>();

        // Act
        var loadPathTask = _sut.LoadProfileSettingsAsync(profileSettings);
        _sut.NotifyMainWindowIsLoaded();
        _sut.NotifyProfilesAreLoaded();
        await loadPathTask;

        // Verify
        var actualProfile = _commandBus.ReceivedResults.Single() as Profile;
        Assert.NotNull(actualProfile);
        _commandBus.AssertNoCommandsButOfType<ProfileLoadAnonymousCommand>();
        _commandBus.AssertSingleCommand<ProfileLoadAnonymousCommand>(
            x => Assert.Equal(profileSettings.LogCodec, x.Settings.LogCodec)
        );
            // TODO: Check other fields x => Assert.Equal(profileSettings.FileArtifactLinesCount, x.Settings.FileArtifactLinesCount));
        _currentProfileMock.Verify(x => x.LoadProfileAsync(actualProfile), Times.Once);
        _mainViewModelMock.VerifySet(x => x.SelectedTabIndex = MAINVIEWMODEL_TABS_COUNT - 1);
        _mainViewModelMock.VerifySet(x => x.IsBusy = false);
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

        // Act
        var task = _sut.AutoLoadProfileAsync();
        _sut.NotifyMainWindowIsLoaded();
        _sut.NotifyProfilesAreLoaded();
        await task;

        // Verify
        Assert.True(executed);
    }

    [Fact]
    public async Task AutoLoadProfileAsync_WhenLoadPathPreviouslyInvoked_Stops()
    {
        // Arrange
        var settings = _fixture.Create<ProfileSettingsBase>();
        _sut.NotifyMainWindowIsLoaded();
        _sut.NotifyProfilesAreLoaded();

        // Pre-Act
        await _sut.LoadProfileSettingsAsync(settings);

        // Act
        await _sut.AutoLoadProfileAsync();

        // Verify
        _settingsQueryMock.Verify(x => x.Get(), Times.Never);
    }

    [StaFact]
    public async Task ShowShareViewAsync_HappyFlowScenario()
    {
        // Arrange
        var items = _fixture.CreateMany<ILogItemViewModel>().ToArray();

        // Act
        await _sut.ShowShareViewAsync(items);

        // Verify
        _uiMock.Verify(x => x.ShowInformation(It.IsAny<string>()), Times.Never);
        _dialogCoordinatorMock.Verify(x => x.ShowMetroDialogAsync(_mainViewModelMock.Object,
            It.Is<CustomDialog>(cd => cd.Content is ShareLogsView), It.IsAny<MetroDialogSettings>()), Times.Once);
    }

    [Fact]
    public async Task ShowShareViewAsync_WhenNoLogsProvided_ThenMessageIsShown()
    {
        // Arrange
        var items = Array.Empty<ILogItemViewModel>();

        // Act
        await _sut.ShowShareViewAsync(items);

        // Verify
        _uiMock.Verify(x => x.ShowInformation(It.IsAny<string>()));
        _dialogCoordinatorMock.Verify(x => x.ShowMetroDialogAsync(It.IsAny<object>(), It.IsAny<BaseMetroDialog>(), It.IsAny<MetroDialogSettings>()), Times.Never);
    }

    private T SetupDummyTabsAnd<T>()
        where T: class, ITabViewModel
    {
        var tab = Mock.Of<T>();
        var allTabs = _fixture.CreateMany<ITabViewModel>(MAINVIEWMODEL_TABS_COUNT - 1).Append(tab).ToImmutableArray();
        _mainViewModelMock.Setup(x => x.Tabs).Returns(allTabs);
        return tab;
    }
}
