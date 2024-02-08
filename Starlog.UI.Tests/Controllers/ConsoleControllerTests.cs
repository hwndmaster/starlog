using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.Core.TestingUtil;
using Genius.Starlog.UI.Console;
using Genius.Starlog.UI.Controllers;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.UI.Tests.Controllers;

public sealed class ConsoleControllerTests
{
    private readonly Mock<ILogCodecContainer> _logCodecContainerMock = new();
    private readonly Mock<IProfileSettingsTemplateQueryService> _templatesQueryMock = new();
    private readonly Mock<IProfileLoadingController> _profileLoadingControllerMock = new();
    private readonly TestLogger<ConsoleController> _logger = new();
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();

    private readonly ConsoleController _sut;

    public ConsoleControllerTests()
    {
        _sut = new(_logCodecContainerMock.Object,
            _templatesQueryMock.Object, _profileLoadingControllerMock.Object,
            _logger);
    }

    [Fact]
    public async Task LoadPathAsync_GivenLoadPathCommandLineOptions_HappyFlowScenario()
    {
        // Arrange
        var options = _fixture.Create<LoadPathCommandLineOptions>();
        var codec = new LogCodec(Guid.NewGuid(), options.Codec!);
        var profileSettings = _fixture.Create<ProfileSettingsBase>();
        var allCodecs = _fixture.CreateMany<LogCodec>().Append(codec);
        var processor = Mock.Of<ILogCodecSettingsReader>(x => x.ReadFromCommandLineArguments(profileSettings, It.Is<string[]>(arg => arg.SequenceEqual(options.CodecSettings))) == true);
        _logCodecContainerMock.Setup(x => x.GetLogCodecs()).Returns(allCodecs);
        _logCodecContainerMock.Setup(x => x.CreateProfileSettings(codec)).Returns(profileSettings);
        _logCodecContainerMock.Setup(x => x.FindLogCodecSettingsReader(profileSettings)).Returns(processor);

        // Act
        await _sut.LoadPathAsync(options);

        // Verify
        _profileLoadingControllerMock.Verify(x => x.LoadProfileSettingsAsync(It.Is<ProfileSettingsBase>(
            ps => ps == profileSettings)), Times.Once);
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
        _profileLoadingControllerMock.Verify(x => x.LoadProfileSettingsAsync(It.IsAny<ProfileSettingsBase>()), Times.Never);
        Assert.Equal(LogLevel.Warning, _logger.Logs.Single().LogLevel);
    }

    [Fact]
    public async Task LoadPathAsync_GivenLoadPathCommandLineOptions_WhenArgumentsCouldNotBeRead_Stops()
    {
        // Arrange
        var options = _fixture.Create<LoadPathCommandLineOptions>();
        var codec = new LogCodec(Guid.NewGuid(), options.Codec!);
        var profileCodec = _fixture.Create<ProfileSettingsBase>();
        var allCodecs = _fixture.CreateMany<LogCodec>().Append(codec);
        var processor = Mock.Of<ILogCodecSettingsReader>(x =>
            x.ReadFromCommandLineArguments(profileCodec, It.Is<string[]>(arg => arg.SequenceEqual(options.CodecSettings))) == false);
        _logCodecContainerMock.Setup(x => x.GetLogCodecs()).Returns(allCodecs);
        _logCodecContainerMock.Setup(x => x.CreateProfileSettings(codec)).Returns(profileCodec);
        _logCodecContainerMock.Setup(x => x.FindLogCodecSettingsReader(profileCodec)).Returns(processor);

        // Act
        await _sut.LoadPathAsync(options);

        // Verify
        _profileLoadingControllerMock.Verify(x => x.LoadProfileSettingsAsync(It.IsAny<ProfileSettingsBase>()), Times.Never);
        Assert.Equal(LogLevel.Warning, _logger.Logs.Single().LogLevel);
    }

    [Fact]
    public async Task LoadPathAsync_WhenProfileSettingsCannotBeCreated_Stops()
    {
        // Arrange
        var options = _fixture.Create<LoadPathCommandLineOptions>();
        var codec = new LogCodec(Guid.NewGuid(), options.Codec!);
        var allCodecs = _fixture.CreateMany<LogCodec>().Append(codec);
        _logCodecContainerMock.Setup(x => x.GetLogCodecs()).Returns(allCodecs);

        // Act
        await _sut.LoadPathAsync(options);

        // Verify
        _profileLoadingControllerMock.Verify(x => x.LoadProfileSettingsAsync(It.IsAny<ProfileSettingsBase>()), Times.Never);
        Assert.Equal(LogLevel.Warning, _logger.Logs.Single().LogLevel);
    }

    [Fact]
    public async Task LoadPathAsync_GivenTemplateName_HappyFlowScenario()
    {
        // Arrange
        var options = _fixture.Create<LoadPathCommandLineOptions>();
        var template = new ProfileSettingsTemplate
        {
            Name = options.Template!,
            Settings = new TestProfileSettings()
        };
        _templatesQueryMock.Setup(x => x.GetAllAsync()).ReturnsAsync([template]);

        // Act
        await _sut.LoadPathAsync(options);

        // Verify
        _profileLoadingControllerMock.Verify(x => x.LoadProfileSettingsAsync(It.Is<TestProfileSettings>(
            y => y.LogCodec == template.Settings.LogCodec && y.IsCloned)), Times.Once);
    }

    [Fact]
    public async Task LoadPathAsync_GivenTemplateId_HappyFlowScenario()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var options = _fixture.Build<LoadPathCommandLineOptions>()
            .With(x => x.Template, templateId.ToString())
            .Create();
        var template = new ProfileSettingsTemplate
        {
            Id = templateId,
            Name = _fixture.Create<string>(),
            Settings = new TestProfileSettings()
        };
        _templatesQueryMock.Setup(x => x.FindByIdAsync(templateId)).ReturnsAsync(template);

        // Act
        await _sut.LoadPathAsync(options);

        // Verify
        _profileLoadingControllerMock.Verify(x => x.LoadProfileSettingsAsync(It.Is<TestProfileSettings>(
            y => y.LogCodec == template.Settings.LogCodec && y.IsCloned)), Times.Once);
    }
}
