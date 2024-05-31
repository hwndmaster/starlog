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
using Genius.Starlog.UI.Views;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.UI.Tests.Controllers;

public sealed class ProfileLoadingControllerTests
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly TestCommandBus _commandBus = new();
    private readonly ICurrentProfile _currentProfileMock = A.Fake<ICurrentProfile>();
    private readonly IDialogCoordinator _dialogCoordinatorMock = A.Fake<IDialogCoordinator>();
    private readonly TestEventBus _eventBus = new();
    private readonly IMainController _mainControllerMock = A.Fake<IMainController>();
    private readonly IMainViewModel _mainViewModelMock = A.Fake<IMainViewModel>();
    private readonly IProfileSettingsViewModelFactory _profileSettingsViewModelFactoryMock = A.Fake<IProfileSettingsViewModelFactory>();
    private readonly ISettingsQueryService _settingsQueryMock = A.Fake<ISettingsQueryService>();
    private readonly TestLogger<ProfileLoadingController> _logger = new();

    private readonly ProfileLoadingController _sut;

    private readonly TaskCompletionSource _loadedTask = new();

    public ProfileLoadingControllerTests()
    {
        A.CallTo(() => _mainControllerMock.Loaded).Returns(_loadedTask.Task);

        _sut = new(_commandBus,
            _currentProfileMock,
            _dialogCoordinatorMock,
            _eventBus,
            _logger,
            _mainControllerMock,
            new Lazy<IMainViewModel>(() => _mainViewModelMock),
            _settingsQueryMock,
            _profileSettingsViewModelFactoryMock);
    }

    [Fact]
    public async Task LoadProfileSettingsAsync_GivenProfileSettings_HappyFlowScenario()
    {
        // Arrange
        var profileSettings = _fixture.Create<ProfileSettingsBase>();
        A.CallTo(() => _currentProfileMock.LoadProfileAsync(A<Profile>.Ignored))
            .Invokes(() =>
            {
                // Ensure the MainViewModel is set to busy
                A.CallTo(() => _mainControllerMock.SetBusy(true)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mainControllerMock.SetBusy(false)).MustNotHaveHappened();
            });
        SetupDummyTabsAnd<ILogsViewModel>();

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
        A.CallTo(() => _currentProfileMock.LoadProfileAsync(actualProfile)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mainControllerMock.ShowLogsTab()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mainControllerMock.SetBusy(false)).MustHaveHappenedOnceExactly();
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
        A.CallTo(() => _settingsQueryMock.Get()).Returns(settings);
        var executed = false;
        var loadProfileCommand = A.Fake<IActionCommand>();
        A.CallTo(() => loadProfileCommand.Execute(null)).Invokes(() => executed = true);
        var profile = A.Fake<IProfileViewModel>();
        A.CallTo(() => profile.Id).Returns(settings.AutoLoadProfile);
        A.CallTo(() => profile.LoadProfileCommand).Returns(loadProfileCommand);
        var allProfiles = new DelayedObservableCollection<IProfileViewModel>(_fixture.CreateMany<IProfileViewModel>().Append(profile));
        var profileTab = SetupDummyTabsAnd<IProfilesViewModel>();
        A.CallTo(() => profileTab.Profiles).Returns(allProfiles);
        A.CallTo(() => _mainControllerMock.GetProfilesTab()).Returns(profileTab);

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
        A.CallTo(() => _settingsQueryMock.Get()).MustNotHaveHappened();
    }

    private T SetupDummyTabsAnd<T>()
        where T: class, ITabViewModel
    {
        var tab = A.Fake<T>();
        var allTabs = _fixture.CreateMany<ITabViewModel>().Append(tab).ToImmutableArray();
        A.CallTo(() => _mainViewModelMock.Tabs).Returns(allTabs);
        return tab;
    }
}
