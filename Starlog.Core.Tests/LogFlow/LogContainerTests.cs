using System.Collections.Immutable;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Atom.Infrastructure.TestingUtil.Events;
using Genius.Atom.Infrastructure.TestingUtil.Io;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core.Tests.LogReading;

public sealed class LogContainerTests
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly TestFileService _fileService = new();
    private readonly TestFileSystemWatcher _fileWatcher = new();
    private readonly TestEventBus _eventBus = new();
    private readonly Mock<ILogReaderContainer> _logReaderContainerMock = new();
    private readonly TestSynchronousScheduler _scheduler = new();
    private readonly Mock<ILogger<LogContainer>> _loggerMock = new();

    private readonly LogContainer _sut;

    public LogContainerTests()
    {
        new SupportMutableValueTypesCustomization().Customize(_fixture);

        _sut = new LogContainer(_eventBus, _fileService, _fileWatcher,
            _logReaderContainerMock.Object, _scheduler, _loggerMock.Object);
    }

    [Fact]
    public async Task LoadProfileAsync_WhenForFolder_ThenProfileLoaded_AndEventsPublished()
    {
        // Arrange
        var profile = _fixture.Create<Profile>();
        var files = SampleFiles(profile);
        var sampleResults = SampleLogResults(files.Length);
        SetupLogReaderResultForFiles(profile, sampleResults, files);

        int logsAddedHandled = 0;
        List<FileRecord> filesAdded = new();
        Profile? profileSelected = null;
        _sut.LogsAdded.Subscribe(_ => logsAddedHandled++);
        _sut.FileAdded.Subscribe(filesAdded.Add);
        _sut.ProfileChanged.Subscribe(x => profileSelected = x);

        // Pre-verify
        Assert.Null(_sut.Profile);
        Assert.False(_fileWatcher.IsListening);

        // Act
        await _sut.LoadProfileAsync(profile);

        // Verify
        filesAdded = filesAdded.OrderBy(x => x.FullPath).ToList();
        _eventBus.AssertNoEventOfType<ProfileLoadingErrorEvent>();
        Assert.Equal(profile, _sut.Profile);
        Assert.Equal(profile, profileSelected);
        Assert.Equal(files.Length, logsAddedHandled);
        Assert.Equal(files.Length, filesAdded.Count);
        Assert.Equivalent(filesAdded, _sut.GetFiles(), strict: true);
        Assert.True(_fileWatcher.IsListening);
        Assert.Equal(profile.Path, _fileWatcher.ListeningPath);
    }

    [Fact]
    public async Task LoadProfileAsync_WhenPathDoesntExist_ThenProfileNotLoaded()
    {
        // Arrange
        var profile = _fixture.Create<Profile>();

        // Act
        await _sut.LoadProfileAsync(profile);

        // Verify
        Assert.Null(_sut.Profile);
        var @event = _eventBus.GetSingleEvent<ProfileLoadingErrorEvent>();
        Assert.Equal(profile, @event.Profile);
    }

    [Fact]
    public async Task CloseProfile_HappyFlowScenario()
    {
        // Arrange
        var profile = _fixture.Create<Profile>();
        var files = SampleFiles(profile);
        var sampleResults = SampleLogResults(files.Length);
        SetupLogReaderResultForFiles(profile, sampleResults, files);
        await _sut.LoadProfileAsync(profile);
        var profileClosed = false;
        _sut.ProfileClosed.Subscribe(_ => profileClosed = true);

        // Pre-Verify
        Assert.NotNull(_sut.Profile);
        Assert.NotEmpty(_sut.GetFiles());
        Assert.NotEmpty(_sut.GetLogs());
        Assert.NotEmpty(_sut.GetLoggers());
        Assert.NotEmpty(_sut.GetLogLevels());
        Assert.NotEmpty(_sut.GetThreads());
        Assert.True(_fileWatcher.IsListening);

        // Act
        _sut.CloseProfile();

        // Verify
        Assert.Null(_sut.Profile);
        Assert.True(profileClosed);
        Assert.Empty(_sut.GetFiles());
        Assert.Empty(_sut.GetLogs());
        Assert.Empty(_sut.GetLoggers());
        Assert.Empty(_sut.GetLogLevels());
        Assert.Empty(_sut.GetThreads());
        Assert.False(_fileWatcher.IsListening);
    }

    [Fact]
    public async Task GetFiles_HappyFlowScenario()
    {
        // Arrange & Pre-verify
        var profile = _fixture.Create<Profile>();
        var files = SampleFiles(profile);
        var sampleResults = SampleLogResults(files.Length);
        SetupLogReaderResultForFiles(profile, sampleResults, files);
        Assert.Empty(_sut.GetFiles());
        await _sut.LoadProfileAsync(profile);

        // Act
        var actual = _sut.GetFiles();

        // Verify
        Assert.Equivalent(files.Select(x => x.fullPath.ToLower()), actual.Select(x => x.FullPath.ToLower()), strict: true);
        Assert.Equivalent(sampleResults.Select(x => x.FileArtifacts), actual.Select(x => x.Artifacts), strict: true);
        Assert.Equivalent(files.Select(x => x.sampleContent.Length), actual.Select(x => (int)x.LastReadOffset), strict: true);
    }

    [Fact]
    public async Task GetLogs_HappyFlowScenario()
    {
        // Arrange & Pre-verify
        var profile = _fixture.Create<Profile>();
        var files = SampleFiles(profile);
        var sampleResults = SampleLogResults(files.Length);
        SetupLogReaderResultForFiles(profile, sampleResults, files);
        Assert.Empty(_sut.GetLogs());
        await _sut.LoadProfileAsync(profile);

        // Act
        var actual = _sut.GetLogs();

        // Verify
        Assert.Equivalent(sampleResults.SelectMany(x => x.Records), actual, strict: true);
    }

    [Fact]
    public async Task GetLoggers_HappyFlowScenario()
    {
        // Arrange & Pre-verify
        var profile = _fixture.Create<Profile>();
        var files = SampleFiles(profile);
        var sampleResults = SampleLogResults(files.Length);
        SetupLogReaderResultForFiles(profile, sampleResults, files);
        Assert.Empty(_sut.GetLoggers());
        await _sut.LoadProfileAsync(profile);

        // Act
        var actual = _sut.GetLoggers();

        // Verify
        Assert.Equivalent(sampleResults.SelectMany(x => x.Loggers), actual, strict: true);
    }

    [Fact]
    public async Task GetLogLevels_HappyFlowScenario()
    {
        // Arrange & Pre-verify
        var profile = _fixture.Create<Profile>();
        var files = SampleFiles(profile);
        var sampleResults = SampleLogResults(files.Length);
        SetupLogReaderResultForFiles(profile, sampleResults, files);
        Assert.Empty(_sut.GetLogLevels());
        await _sut.LoadProfileAsync(profile);

        // Act
        var actual = _sut.GetLogLevels();

        // Verify
        Assert.Equivalent(sampleResults.SelectMany(x => x.LogLevels), actual, strict: true);
    }

    [Fact]
    public async Task GetThreads_HappyFlowScenario()
    {
        // Arrange & Pre-verify
        var profile = _fixture.Create<Profile>();
        var files = SampleFiles(profile);
        var sampleResults = SampleLogResults(files.Length);
        SetupLogReaderResultForFiles(profile, sampleResults, files);
        Assert.Empty(_sut.GetLogLevels());
        await _sut.LoadProfileAsync(profile);

        // Act
        var actual = _sut.GetThreads();

        // Verify
        var expected = sampleResults.SelectMany(x => x.Records.Select(y => y.Thread));
        Assert.Equivalent(expected, actual, strict: true);
    }

    // TODO: Finish the test
    [Fact]
    public void FileWatcher_CreatedOrChanged_()
    {
        // Arrange

        // Act
        _sut.CloseProfile();

        // Verify
        Assert.Fail("Not implemented yet");
    }

    // TODO: Finish the test
    [Fact]
    public void FileWatcher_Renamed_()
    {
        // Arrange

        // Act
        _sut.CloseProfile();

        // Verify
        Assert.Fail("Not implemented yet");
    }

    // TODO: Finish the test
    [Fact]
    public void FileWatcher_Error_()
    {
        // Arrange

        // Act
        _sut.CloseProfile();

        // Verify
        Assert.Fail("Not implemented yet");
    }

    private (string fullPath, byte[] sampleContent)[] SampleFiles(Profile profile)
    {
        return _fixture.CreateMany<string>().Select(fileName =>
        {
            var sampleContent = _fixture.CreateMany<byte>(_fixture.Create<int>()).ToArray();
            var fullPath = Path.Combine(profile.Path, fileName);
            _fileService.AddFile(fullPath, sampleContent);
            return (fullPath, sampleContent);
        }).OrderBy(x => x.fullPath).ToArray();
    }

    private void SetupLogReaderResultForFiles(Profile profile, LogReaderResult[] results, (string fullPath, byte[] sampleContent)[] files)
    {
        var logReaderProcessorMock = new Mock<ILogReaderProcessor>();

        foreach (var (result, file) in results.Zip(files))
        {
            logReaderProcessorMock.Setup(x => x.ReadAsync(profile, It.Is<FileRecord>(fr => fr.FullPath.Equals(file.fullPath, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Stream>(), profile.FileArtifactLinesCount > 0))
            .ReturnsAsync(result)
            .Callback((Profile _, FileRecord _, Stream stream, bool _)
                => stream.Read(new byte[file.sampleContent.Length], 0, file.sampleContent.Length));
        }

        _logReaderContainerMock.Setup(x => x.CreateLogReaderProcessor(profile.LogReader))
            .Returns(() => logReaderProcessorMock.Object);
    }

    private LogReaderResult[] SampleLogResults(int count)
    {
        return Enumerable.Range(1, count)
            .Select(_ => new LogReaderResult(_fixture.Create<FileArtifacts>(),
                ImmutableArray.Create(_fixture.CreateMany<LogRecord>().ToArray()),
                _fixture.CreateMany<LoggerRecord>().ToArray(),
                _fixture.CreateMany<LogLevelRecord>().ToArray())).ToArray();
    }
}
