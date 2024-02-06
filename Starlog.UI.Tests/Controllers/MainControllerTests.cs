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
    private readonly Mock<IClipboardHelper> _clipboardHelperMock = new();
    private readonly Mock<IDialogCoordinator> _dialogCoordinatorMock = new();
    private readonly Mock<IUserInteraction> _uiMock = new();
    private readonly Mock<IMainViewModel> _mainViewModelMock = new();
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();

    private readonly MainController _sut;

    public MainControllerTests()
    {
        _sut = new(_clipboardHelperMock.Object,
            _dialogCoordinatorMock.Object,
            new TestFileService(),
            _uiMock.Object,
            new Lazy<IMainViewModel>(() => _mainViewModelMock.Object));
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
}
