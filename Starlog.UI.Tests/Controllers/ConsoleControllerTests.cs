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
    private readonly ILogCodecContainer _logCodecContainerMock = A.Fake<ILogCodecContainer>();
    private readonly IProfileSettingsTemplateQueryService _templatesQueryMock = A.Fake<IProfileSettingsTemplateQueryService>();
    private readonly IProfileLoadingController _profileLoadingControllerMock = A.Fake<IProfileLoadingController>();
    private readonly TestLogger<ConsoleController> _logger = new();
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();

    private readonly ConsoleController _sut;

    public ConsoleControllerTests()
    {
        _sut = new(_logCodecContainerMock,
            _templatesQueryMock, _profileLoadingControllerMock,
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
        var processor = A.Fake<ILogCodecSettingsReader>();
        A.CallTo(() => processor.ReadFromCommandLineArguments(profileSettings, A<string[]>.That.Matches(arg => arg.SequenceEqual(options.CodecSettings)))).Returns(true);
        A.CallTo(() => _logCodecContainerMock.GetLogCodecs()).Returns(allCodecs);
        A.CallTo(() => _logCodecContainerMock.CreateProfileSettings(codec)).Returns(profileSettings);
        A.CallTo(() => _logCodecContainerMock.FindLogCodecSettingsReader(profileSettings)).Returns(processor);

        // Act
        await _sut.LoadPathAsync(options);

        // Verify
        A.CallTo(() => _profileLoadingControllerMock.LoadProfileSettingsAsync(
            A<ProfileSettingsBase>.That.Matches(ps => ps == profileSettings)))
            .MustHaveHappenedOnceExactly();
        Assert.DoesNotContain(_logger.Logs, x => x.LogLevel == LogLevel.Warning);
    }

    [Fact]
    public async Task LoadPathAsync_GivenLoadPathCommandLineOptions_WhenCodecIsUnknown_Stops()
    {
        // Arrange
        var options = _fixture.Create<LoadPathCommandLineOptions>();
        A.CallTo(() => _logCodecContainerMock.GetLogCodecs())
            .Returns(_fixture.CreateMany<LogCodec>()); // LogCodec from options is not included.

        // Act
        await _sut.LoadPathAsync(options);

        // Verify
        A.CallTo(() => _profileLoadingControllerMock.LoadProfileSettingsAsync(A<ProfileSettingsBase>.Ignored)).MustNotHaveHappened();
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
        var processor = A.Fake<ILogCodecSettingsReader>();
        A.CallTo(() => processor.ReadFromCommandLineArguments(profileCodec, A<string[]>.That.Matches(arg => arg.SequenceEqual(options.CodecSettings))))
            .Returns(false);
        A.CallTo(() => _logCodecContainerMock.GetLogCodecs()).Returns(allCodecs);
        A.CallTo(() => _logCodecContainerMock.CreateProfileSettings(codec)).Returns(profileCodec);
        A.CallTo(() => _logCodecContainerMock.FindLogCodecSettingsReader(profileCodec)).Returns(processor);

        // Act
        await _sut.LoadPathAsync(options);

        // Verify
        A.CallTo(() => _profileLoadingControllerMock.LoadProfileSettingsAsync(A<ProfileSettingsBase>.Ignored))
            .MustNotHaveHappened();
        Assert.Equal(LogLevel.Warning, _logger.Logs.Single().LogLevel);
    }

    [Fact]
    public async Task LoadPathAsync_WhenProfileSettingsCannotBeCreated_Stops()
    {
        // Arrange
        var options = _fixture.Create<LoadPathCommandLineOptions>();
        var codec = new LogCodec(Guid.NewGuid(), options.Codec!);
        var allCodecs = _fixture.CreateMany<LogCodec>().Append(codec);
        A.CallTo(() => _logCodecContainerMock.GetLogCodecs()).Returns(allCodecs);

        // Act
        await _sut.LoadPathAsync(options);

        // Verify
        A.CallTo(() => _profileLoadingControllerMock.LoadProfileSettingsAsync(A<ProfileSettingsBase>.Ignored)).MustNotHaveHappened();
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
        A.CallTo(() => _templatesQueryMock.GetAllAsync()).Returns([template]);

        // Act
        await _sut.LoadPathAsync(options);

        // Verify
        A.CallTo(() => _profileLoadingControllerMock.LoadProfileSettingsAsync(A<TestProfileSettings>.That.Matches(
            y => y.LogCodec == template.Settings.LogCodec && y.IsCloned))).MustHaveHappenedOnceExactly();
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
        A.CallTo(() => _templatesQueryMock.FindByIdAsync(templateId)).Returns(template);

        // Act
        await _sut.LoadPathAsync(options);

        // Verify
        A.CallTo(() => _profileLoadingControllerMock.LoadProfileSettingsAsync(A<TestProfileSettings>.That.Matches(
            y => y.LogCodec == template.Settings.LogCodec && y.IsCloned))).MustHaveHappenedOnceExactly();
    }
}
