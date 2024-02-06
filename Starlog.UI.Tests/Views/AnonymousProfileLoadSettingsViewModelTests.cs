using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Views;
using Genius.Starlog.UI.Views.ProfileSettings;

namespace Genius.Starlog.UI.Tests.Views;

public sealed class AnonymousProfileLoadSettingsViewModelTests
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture();
    private Mock<ILogCodecContainer> _logCodecContainerMock = new();
    private readonly AnonymousProfileLoadSettingsViewModel _sut;
    private bool _closedHandled = false;
    private ProfileSettingsBase _profileSettings;
    private ProfileSettingsBase? _profileSettingsConfirmed;

    public AnonymousProfileLoadSettingsViewModelTests()
    {
        _profileSettings = _fixture.Create<ProfileSettingsBase>();

        var profileSettingsVm = new Mock<IProfileSettingsViewModel>();
        profileSettingsVm.Setup(x => x.CommitChanges()).Returns(_profileSettings);

        var logCodec = new LogCodec(Guid.NewGuid(), PlainTextProfileSettings.CodecName);
        var profileSettings = new PlainTextProfileSettings(logCodec)
        {
            Path = _fixture.Create<string>()
        };
        _logCodecContainerMock.Setup(x => x.GetLogCodecs()).Returns([logCodec]);
        _logCodecContainerMock.Setup(x => x.CreateProfileSettings(logCodec)).Returns(profileSettings);

        var viewModelFactoryMock = new Mock<IViewModelFactory>();
        viewModelFactoryMock.Setup(x => x.CreateProfileSettings(profileSettings)).Returns(profileSettingsVm.Object);

        var closeCommand = new ActionCommand(_ => _closedHandled = true);
        var confirmCommand = new ActionCommand<ProfileSettingsBase>(arg => _profileSettingsConfirmed = arg);

        _sut = new AnonymousProfileLoadSettingsViewModel(
            _logCodecContainerMock.Object,
            viewModelFactoryMock.Object,
            _fixture.Create<string>(),
            closeCommand,
            confirmCommand);
    }

    [Fact]
    public void CloseCommand_HappyFlowScenario()
    {
        // Act
        _sut.CloseCommand.Execute(null);

        // Verify
        Assert.True(_closedHandled);
        Assert.Null(_profileSettingsConfirmed);
    }

    [Fact]
    public void ConfirmCommand_HappyFlowScenario()
    {
        // Act
        _sut.ConfirmCommand.Execute(null);

        // Verify
        Assert.True(_closedHandled);
        Assert.Equal(_profileSettings, _profileSettingsConfirmed);
    }
}
