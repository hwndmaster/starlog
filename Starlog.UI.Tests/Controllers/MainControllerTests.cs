using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Atom.Infrastructure.TestingUtil.Io;
using Genius.Atom.UI.Forms;
using Genius.Starlog.UI.Controllers;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views;
using MahApps.Metro.Controls.Dialogs;

namespace Genius.Starlog.UI.Tests.Controllers;

public sealed class MainControllerTests
{
    private readonly IClipboardHelper _clipboardHelperMock = A.Fake<IClipboardHelper>();
    private readonly IDialogCoordinator _dialogCoordinatorMock = A.Fake<IDialogCoordinator>();
    private readonly IUserInteraction _uiMock = A.Fake<IUserInteraction>();
    private readonly IMainViewModel _mainViewModelMock = A.Fake<IMainViewModel>();
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();

    private readonly MainController _sut;

    public MainControllerTests()
    {
        _sut = new(_clipboardHelperMock,
            _dialogCoordinatorMock,
            new TestFileService(),
            _uiMock,
            new Lazy<IMainViewModel>(() => _mainViewModelMock));
    }

    [StaFact]
    public async Task ShowShareViewAsync_HappyFlowScenario()
    {
        // Arrange
        var items = _fixture.CreateMany<ILogItemViewModel>().ToArray();

        // Act
        await _sut.ShowShareViewAsync(items);

        // Verify
        A.CallTo(() => _uiMock.ShowInformation(A<string>.Ignored)).MustNotHaveHappened();
        A.CallTo(() => _dialogCoordinatorMock.ShowMetroDialogAsync(_mainViewModelMock,
            A<CustomDialog>.That.Matches(cd => cd.Content is ShareLogsView), A<MetroDialogSettings>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ShowShareViewAsync_WhenNoLogsProvided_ThenMessageIsShown()
    {
        // Arrange
        var items = Array.Empty<ILogItemViewModel>();

        // Act
        await _sut.ShowShareViewAsync(items);

        // Verify
        A.CallTo(() => _uiMock.ShowInformation(A<string>.Ignored));
        A.CallTo(() => _dialogCoordinatorMock.ShowMetroDialogAsync(A<object>.Ignored, A<BaseMetroDialog>.Ignored, A<MetroDialogSettings>.Ignored)).MustNotHaveHappened();
    }
}
