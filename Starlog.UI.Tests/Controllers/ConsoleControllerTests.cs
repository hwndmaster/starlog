using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Console;
using Genius.Starlog.UI.Controllers;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.UI.Tests.Controllers;

public sealed class ConsoleControllerTests
{
    private readonly Mock<ILogCodecContainer> _logCodecContainerMock = new();
    private readonly Mock<IProfileSettingsTemplateQueryService> _templatesQueryMock = new();
    private readonly Mock<IMainController> _mainControllerMock = new();
    private readonly TestLogger<ConsoleController> _logger = new();
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();

    private readonly ConsoleController _sut;

    public ConsoleControllerTests()
    {
        _sut = new(_logCodecContainerMock.Object,
            _templatesQueryMock.Object, _mainControllerMock.Object,
            _logger);
    }

    [Fact]
    public async Task LoadPathAsync_GivenLoadPathCommandLineOptions_HappyFlowScenario()
    {
        // Arrange
        var options = _fixture.Create<LoadPathCommandLineOptions>();
        var codec = new LogCodec(Guid.NewGuid(), options.Codec!);
        var profileCodec = _fixture.Create<ProfileLogCodecBase>();
        var allCodecs = _fixture.CreateMany<LogCodec>().Append(codec);
        var processor = Mock.Of<ILogCodecProcessor>(x => x.ReadFromCommandLineArguments(profileCodec, It.Is<string[]>(arg => arg.SequenceEqual(options.CodecSettings))) == true);
        _logCodecContainerMock.Setup(x => x.GetLogCodecs()).Returns(allCodecs);
        _logCodecContainerMock.Setup(x => x.CreateProfileLogCodec(codec)).Returns(profileCodec);
        _logCodecContainerMock.Setup(x => x.CreateLogCodecProcessor(profileCodec)).Returns(processor);

        // Act
        await _sut.LoadPathAsync(options);

        // Verify
        _mainControllerMock.Verify(x => x.LoadPathAsync(options.Path, It.Is<ProfileSettings>(ps =>
            ps.LogCodec == profileCodec
            && ps.FileArtifactLinesCount == options.FileArtifactLinesCount)), Times.Once);
        Assert.DoesNotContain(_logger.Logs, x => x.LogLevel == LogLevel.Warning);
    }

    [Fact]
    public async Task LoadPathAsync_GivenLoadPathCommandLineOptions_WhenCodecIsUnknown_Stops()
    {
        // Arrange
        var options = _fixture.Create<LoadPathCommandLineOptions>();
        _logCodecContainerMock.Setup(x => x.GetLogCodecs())
            .Returns(_fixture.CreateMany<LogCodec>()); // LogCodec from options is not included.

        // Act
        await _sut.LoadPathAsync(options);

        // Verify
        _mainControllerMock.Verify(x => x.LoadPathAsync(It.IsAny<string>(), It.IsAny<ProfileSettings>()), Times.Never);
        Assert.Equal(LogLevel.Warning, _logger.Logs.Single().LogLevel);
    }

    [Fact]
    public async Task LoadPathAsync_GivenLoadPathCommandLineOptions_WhenArgumentsCouldNotBeRead_Stops()
    {
        // Arrange
        var options = _fixture.Create<LoadPathCommandLineOptions>();
        var codec = new LogCodec(Guid.NewGuid(), options.Codec!);
        var profileCodec = _fixture.Create<ProfileLogCodecBase>();
        var allCodecs = _fixture.CreateMany<LogCodec>().Append(codec);
        var processor = Mock.Of<ILogCodecProcessor>(x =>
            x.ReadFromCommandLineArguments(profileCodec, It.Is<string[]>(arg => arg.SequenceEqual(options.CodecSettings))) == false);
        _logCodecContainerMock.Setup(x => x.GetLogCodecs()).Returns(allCodecs);
        _logCodecContainerMock.Setup(x => x.CreateProfileLogCodec(codec)).Returns(profileCodec);
        _logCodecContainerMock.Setup(x => x.CreateLogCodecProcessor(profileCodec)).Returns(processor);

        // Act
        await _sut.LoadPathAsync(options);

        // Verify
        _mainControllerMock.Verify(x => x.LoadPathAsync(It.IsAny<string>(), It.IsAny<ProfileSettings>()), Times.Never);
        Assert.Equal(LogLevel.Warning, _logger.Logs.Single().LogLevel);
    }
}
