using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Views;
using Genius.Starlog.UI.Views.ProfileSettings;

namespace Genius.Starlog.UI.Tests.Views;

public sealed class AnonymousProfileLoadSettingsViewModelTests
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly ILogCodecContainer _logCodecContainerFake = A.Fake<ILogCodecContainer>();
    private readonly AnonymousProfileLoadSettingsViewModel _sut;
    private bool _closedHandled = false;
    private readonly ProfileSettingsBase _profileSettings;
    private ProfileSettingsBase? _profileSettingsConfirmed;

    public AnonymousProfileLoadSettingsViewModelTests()
    {
        _profileSettings = _fixture.Create<ProfileSettingsBase>();

        var profileSettingsVm = A.Fake<IProfileSettingsViewModel>();
        A.CallTo(() => profileSettingsVm.CommitChanges()).Returns(_profileSettings);

        var logCodec = new LogCodec(Guid.NewGuid(), PlainTextProfileSettings.CodecName);
        var profileSettings = new PlainTextProfileSettings(logCodec)
        {
            Path = _fixture.Create<string>()
        };
        A.CallTo(() => _logCodecContainerFake.GetLogCodecs()).Returns([logCodec]);
        A.CallTo(() => _logCodecContainerFake.CreateProfileSettings(logCodec)).Returns(profileSettings);

        var vmFactoryMock = A.Fake<IProfileSettingsViewModelFactory>();
        A.CallTo(() => vmFactoryMock.CreateProfileSettings(profileSettings)).Returns(profileSettingsVm);

        var closeCommand = new ActionCommand(_ => _closedHandled = true);
        var confirmCommand = new ActionCommand<ProfileSettingsBase>(arg => _profileSettingsConfirmed = arg);

        _sut = new AnonymousProfileLoadSettingsViewModel(
            _logCodecContainerFake,
            vmFactoryMock,
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
