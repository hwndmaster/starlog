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
    private record FileWithContentRecord(string FullPath, byte[] Content, LogReaderResult Result);

    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly TestFileService _fileService = new();
    private readonly TestFileSystemWatcher _fileWatcher = new();
    private readonly TestEventBus _eventBus = new();
    private readonly TestSynchronousScheduler _scheduler = new();
    private readonly TestLogger<LogContainer> _logger = new();
    private readonly Mock<ILogReaderContainer> _logReaderContainerMock = new();

    private readonly LogContainer _sut;

    private readonly Profile _sampleProfile;
    private readonly Mock<ILogReaderProcessor> _logReaderProcessorMock;

    public LogContainerTests()
    {
        new SupportMutableValueTypesCustomization().Customize(_fixture);

        _sut = new LogContainer(_eventBus, _fileService, _fileWatcher,
            _logReaderContainerMock.Object, _scheduler, _logger);

        _sampleProfile = _fixture.Create<Profile>();
        _logReaderProcessorMock = new Mock<ILogReaderProcessor>();
        _logReaderContainerMock.Setup(x => x.CreateLogReaderProcessor(_sampleProfile.LogReader))
            .Returns(() => _logReaderProcessorMock.Object);
    }

    [Fact]
    public async Task LoadProfileAsync_WhenForFolder_ThenProfileLoaded_AndEventsPublished()
    {
        // Arrange
        var files = SampleFiles(_sampleProfile);

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
        await _sut.LoadProfileAsync(_sampleProfile);

        // Verify
        filesAdded = filesAdded.OrderBy(x => x.FullPath).ToList();
        _eventBus.AssertNoEventOfType<ProfileLoadingErrorEvent>();
        Assert.Equal(_sampleProfile, _sut.Profile);
        Assert.Equal(_sampleProfile, profileSelected);
        Assert.Equal(files.Length, logsAddedHandled);
        Assert.Equal(files.Length, filesAdded.Count);
        Assert.Equivalent(filesAdded, _sut.GetFiles(), strict: true);
        Assert.True(_fileWatcher.IsListening);
        Assert.Equal(_sampleProfile.Path, _fileWatcher.ListeningPath);
    }

    [Fact]
    public async Task LoadProfileAsync_WhenPathDoesntExist_ThenProfileNotLoaded()
    {
        // Act
        await _sut.LoadProfileAsync(_sampleProfile);

        // Verify
        Assert.Null(_sut.Profile);
        var @event = _eventBus.GetSingleEvent<ProfileLoadingErrorEvent>();
        Assert.Equal(_sampleProfile, @event.Profile);
    }

    [Fact]
    public async Task CloseProfile_HappyFlowScenario()
    {
        // Arrange
        var files = SampleFiles(_sampleProfile);
        await _sut.LoadProfileAsync(_sampleProfile);
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
        var files = SampleFiles(_sampleProfile);
        Assert.Empty(_sut.GetFiles());
        await _sut.LoadProfileAsync(_sampleProfile);

        // Act
        var actual = _sut.GetFiles()
            .OrderBy(x => x.FullPath)
            .ToArray();

        // Verify
        Assert.Equal(files.Length, actual.Length);
        for (var i = 0; i < files.Length; i++)
        {
            Assert.Equal(files[i].FullPath, actual[i].FullPath);
            Assert.Equal(files[i].Result.FileArtifacts, actual[i].Artifacts);
            Assert.Equal(files[i].Content.Length, (int)actual[i].LastReadOffset);
        }
    }

    [Fact]
    public async Task GetLogs_HappyFlowScenario()
    {
        // Arrange & Pre-verify
        var files = SampleFiles(_sampleProfile);
        Assert.Empty(_sut.GetLogs());
        await _sut.LoadProfileAsync(_sampleProfile);

        // Act
        var actual = _sut.GetLogs();

        // Verify
        AssertLogRecords(files.SelectMany(x => x.Result.Records), actual);
    }

    [Fact]
    public async Task GetLoggers_HappyFlowScenario()
    {
        // Arrange & Pre-verify
        var files = SampleFiles(_sampleProfile);
        Assert.Empty(_sut.GetLoggers());
        await _sut.LoadProfileAsync(_sampleProfile);

        // Act
        var actual = _sut.GetLoggers();

        // Verify
        Assert.Equivalent(files.SelectMany(x => x.Result.Loggers), actual, strict: true);
    }

    [Fact]
    public async Task GetLogLevels_HappyFlowScenario()
    {
        // Arrange & Pre-verify
        var files = SampleFiles(_sampleProfile);
        Assert.Empty(_sut.GetLogLevels());
        await _sut.LoadProfileAsync(_sampleProfile);

        // Act
        var actual = _sut.GetLogLevels();

        // Verify
        Assert.Equivalent(files.SelectMany(x => x.Result.LogLevels), actual, strict: true);
    }

    [Fact]
    public async Task GetThreads_HappyFlowScenario()
    {
        // Arrange & Pre-verify
        var files = SampleFiles(_sampleProfile);
        Assert.Empty(_sut.GetLogLevels());
        await _sut.LoadProfileAsync(_sampleProfile);

        // Act
        var actual = _sut.GetThreads();

        // Verify
        var expected = files.SelectMany(x => x.Result.Records.Select(y => y.Thread));
        Assert.Equivalent(expected, actual, strict: true);
    }

    [Fact]
    public async Task FileWatcher_CreatedOrChanged_WhenFileCreated_ThenLoaded()
    {
        // Arrange
        var files = SampleFiles(_sampleProfile);
        await _sut.LoadProfileAsync(_sampleProfile);

        var addedFile = SampleFile(_sampleProfile, _sampleProfile.FileArtifactLinesCount > 0);
        List<LogRecord> logsAdded = new();
        List<FileRecord> filesAdded = new();
        _sut.LogsAdded.Subscribe(logs => logsAdded.AddRange(logs));
        _sut.FileAdded.Subscribe(filesAdded.Add);

        // Act
        _fileWatcher.OnCreated(_sampleProfile.Path, Path.GetFileName(addedFile.FullPath));

        // Verify
        Assert.Single(filesAdded);
        Assert.Equal(addedFile.FullPath, filesAdded[0].FullPath);
        Assert.Contains(_sut.GetFiles(), x => x.FullPath.Equals(addedFile.FullPath));
        AssertLogRecords(addedFile.Result.Records, logsAdded);
    }

    [Fact]
    public async Task FileWatcher_CreatedOrChanged_WhenFileUpdated_ThenLoaded()
    {
        // Arrange
        var files = SampleFiles(_sampleProfile);
        await _sut.LoadProfileAsync(_sampleProfile);

        List<LogRecord> logsAdded = new();
        bool anyFileAdded = false;
        _sut.LogsAdded.Subscribe(logs => logsAdded.AddRange(logs));
        _sut.FileAdded.Subscribe(_ => anyFileAdded = true);
        var initialLastReadOffset = files[0].Content.Length;
        files[0] = files[0] with {
            Content = files[0].Content.Concat(_fixture.CreateMany<byte>(_fixture.Create<int>())).ToArray(),
            Result = SampleLogReaderResult(files[0].FullPath)
        };
        _fileService.AddFile(files[0].FullPath, files[0].Content);
        SetupProcessorRead(_sampleProfile, files[0], false, verifyLastReadOffset: initialLastReadOffset);
        var initialLogs = _sut.GetLogs();

        // Act
        _fileWatcher.OnChanged(_sampleProfile.Path, Path.GetFileName(files[0].FullPath));

        // Verify
        Assert.False(anyFileAdded);
        var actualLogs = _sut.GetLogs();
        AssertLogRecords(files[0].Result.Records, logsAdded);
        AssertLogRecords(initialLogs.Concat(logsAdded), actualLogs);
        var actualFile = _sut.GetFiles().SingleOrDefault(x => x.FullPath.Equals(files[0].FullPath, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(actualFile);
        Assert.Equal(files[0].Content.Length, actualFile.LastReadOffset);
    }

    [Fact]
    public async Task FileWatcher_Renamed_ThenRenamedInLocalCollections()
    {
        // Arrange
        var files = SampleFiles(_sampleProfile);
        await _sut.LoadProfileAsync(_sampleProfile);
        var oldFilePath = files[0].FullPath;
        var newFilePath = Path.Combine(_sampleProfile.Path, _fixture.Create<string>());
        _fileService.RenameFile(files[0].FullPath, newFilePath);
        files[0] = files[0] with { FullPath = newFilePath };
        FileRecord? fileRenamedEventOldRecord = null, fileRenamedEventNewRecord = null;
        _sut.FileRenamed.Subscribe(x => (fileRenamedEventOldRecord, fileRenamedEventNewRecord) = (x.OldRecord, x.NewRecord));

        // Act
        _fileWatcher.OnRenamed(_sampleProfile.Path, Path.GetFileName(newFilePath), Path.GetFileName(oldFilePath));

        // Verify
        Assert.NotNull(fileRenamedEventOldRecord);
        Assert.NotNull(fileRenamedEventNewRecord);
        Assert.Equal(oldFilePath, fileRenamedEventOldRecord.FullPath);
        Assert.Equal(newFilePath, fileRenamedEventNewRecord.FullPath);
        var actualFiles = _sut.GetFiles();
        Assert.DoesNotContain(actualFiles, x => x.FullPath.Equals(oldFilePath));
        Assert.Contains(actualFiles, x => x.FullPath.Equals(newFilePath));
        var actualLogs = _sut.GetLogs();
        Assert.DoesNotContain(actualLogs, x => x.File.FullPath.Equals(oldFilePath));
        Assert.Contains(actualLogs, x => x.File.FullPath.Equals(newFilePath));
    }

    [Fact]
    public void FileWatcher_Error_ThenHandledWithLogger()
    {
        // Arrange
        var exception = new Exception(_fixture.Create<string>());

        // Act
        _fileWatcher.OnError(exception);

        // Verify
        _logger.Logs.Any(x => x.LogLevel == LogLevel.Error
            && x.Message.Equals(exception.Message)
            && x.Exception == exception);
    }

    private static void AssertLogRecords(IEnumerable<LogRecord> expected, IEnumerable<LogRecord> actual)
    {
        var expectedOrdered = expected.OrderBy(x => x.DateTime).ToArray();
        var actualOrdered = actual.OrderBy(x => x.DateTime).ToArray();
        Assert.Equal(expectedOrdered.Length, actualOrdered.Length);
        foreach (var (expectedItem, actualItem) in expectedOrdered.Zip(actualOrdered))
        {
            Assert.Equal(expectedItem.DateTime, actualItem.DateTime);
            Assert.Equal(expectedItem.File.FullPath, actualItem.File.FullPath);
            Assert.Equal(expectedItem.Level, actualItem.Level);
            Assert.Equal(expectedItem.Logger, actualItem.Logger);
            Assert.Equal(expectedItem.Message, actualItem.Message);
            Assert.Equal(expectedItem.Thread, actualItem.Thread);
            Assert.Equal(expectedItem.LogArtifacts, actualItem.LogArtifacts);
        }
    }

    private FileWithContentRecord[] SampleFiles(Profile profile)
    {
        return Enumerable.Range(1, 3).Select(_ => SampleFile(profile, true)).OrderBy(x => x.FullPath).ToArray();
    }

    private FileWithContentRecord SampleFile(Profile profile, bool readFileArtifacts)
    {
        var fileName = _fixture.Create<string>();
        var sampleContent = _fixture.CreateMany<byte>(_fixture.Create<int>()).ToArray();
        var fullPath = Path.Combine(profile.Path, fileName);
        _fileService.AddFile(fullPath, sampleContent);
        var sampleResults = SampleLogReaderResult(fullPath);
        var record = new FileWithContentRecord(fullPath, sampleContent, sampleResults);
        SetupProcessorRead(profile, record, readFileArtifacts);
        return record;
    }

    private LogReaderResult SampleLogReaderResult(string fullPath)
    {
        return new LogReaderResult(_fixture.Create<FileArtifacts>(),
            ImmutableArray.Create(_fixture.Build<LogRecord>()
                .With(x => x.File, new FileRecord(fullPath, 0))
                .CreateMany().ToArray()),
            _fixture.CreateMany<LoggerRecord>().ToArray(),
            _fixture.CreateMany<LogLevelRecord>().ToArray());
    }

    private void SetupProcessorRead(Profile profile, FileWithContentRecord record, bool readFileArtifacts, int? verifyLastReadOffset = null)
    {
        _logReaderProcessorMock.Setup(x => x.ReadAsync(profile,
                It.Is<FileRecord>(fr => fr.FullPath.Equals(record.FullPath, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Stream>(),
                It.Is<LogReadingSettings>(s => s.ReadFileArtifacts == readFileArtifacts)))
            .ReturnsAsync((Profile _, FileRecord fileRecord, Stream stream, LogReadingSettings _) =>
            {
                if (verifyLastReadOffset is not null)
                {
                    Assert.Equal(verifyLastReadOffset, (int)stream.Position);
                }

                stream.Read(new byte[stream.Length], 0, (int)stream.Length);
                return record.Result with {
                    Records = record.Result.Records
                        .Select(r => r with { File = fileRecord })
                        .ToImmutableArray()
                };
            });
    }
}
