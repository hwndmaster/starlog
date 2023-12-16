using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.Tests.Controllers;

public sealed class AnonymousProfileLoadSettingsViewModelTests
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly AnonymousProfileLoadSettingsViewModel _sut;
    private bool _closedHandled = false;
    private ProfileSettings _profileSettings;
    private ProfileSettings? _profileSettingsConfirmed;

    public AnonymousProfileLoadSettingsViewModelTests()
    {
        _profileSettings = new ProfileSettings
        {
            LogCodec = _fixture.Create<ProfileLogCodecBase>()
        };

        var profileSettingsVm = new Mock<IProfileSettingsViewModel>();
        profileSettingsVm.Setup(x => x.CommitChanges()).Returns(_profileSettings);

        var viewModelFactoryMock = new Mock<IViewModelFactory>();
        viewModelFactoryMock.Setup(x => x.CreateProfileSettings(null)).Returns(profileSettingsVm.Object);
        var closeCommand = new ActionCommand(_ => _closedHandled = true);
        var confirmCommand = new ActionCommand<ProfileSettings>(arg => _profileSettingsConfirmed = arg);
        _sut = new AnonymousProfileLoadSettingsViewModel(viewModelFactoryMock.Object, closeCommand, confirmCommand);
    }

    [Fact]
    public async Task CloseCommand_HappyFlowScenario()
    {
        // Arrange

        // Act
        _sut.CloseCommand.Execute(null);

        // Verify
        Assert.True(_closedHandled);
        Assert.Null(_profileSettingsConfirmed);
    }

    [Fact]
    public async Task ConfirmCommand_HappyFlowScenario()
    {
        // Arrange

        // Act
        _sut.ConfirmCommand.Execute(null);

        // Verify
        Assert.True(_closedHandled);
        Assert.Equal(_profileSettings, _profileSettingsConfirmed);
    }
}
