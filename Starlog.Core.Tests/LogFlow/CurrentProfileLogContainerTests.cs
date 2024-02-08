using System.Collections.Immutable;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Atom.Infrastructure.TestingUtil.Events;
using Genius.Atom.Infrastructure.TestingUtil.Io;
using Genius.Atom.Infrastructure.TestingUtil.Tasks;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.ProfileLoading;
using Genius.Starlog.Core.TestingUtil;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core.Tests.LogFlow;

// This test is not fully 'unit' due to the use of `ProfileLoaderFactory`
[Trait(TestCategory.Trait, TestCategory.Integration)]
public sealed class CurrentProfileLogContainerTests : IDisposable
{
    private record FileWithContentRecord(string FullPath, byte[] Content, LogReadingResult Result);

    private const string LOGFILE_EXTENSION = ".log";

    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly TestDirectoryMonitor _directoryMonitor = new();
    private readonly TestFileService _fileService = new();
    private readonly TestFileSystemWatcherFactory _fileWatcherFactory = new();
    private readonly TestEventBus _eventBus = new();
    private readonly TestSynchronousScheduler _scheduler = new();
    private readonly TestLogger<LogContainer> _logger = new();
    private readonly Mock<ILogCodecContainerInternal> _logCodecContainerMock = new();

    private readonly CurrentProfileLogContainer _sut;

    private readonly Profile _sampleProfile;
    private readonly Mock<ILogCodecProcessor> _logCodecProcessorMock;

    public CurrentProfileLogContainerTests()
    {
        new SupportMutableValueTypesCustomization().Customize(_fixture);

        var profileLoaderFactory = new ProfileLoaderFactory(_directoryMonitor, _eventBus, _fileService, _fileWatcherFactory,
            _logCodecContainerMock.Object, new TestLogger<FileBasedProfileLoader>(), _scheduler);
        _sut = new CurrentProfileLogContainer(_eventBus, profileLoaderFactory);

        _sampleProfile = new Profile
        {
            Name = _fixture.Create<string>(),
            Settings = new PlainTextProfileSettings(_fixture.Create<LogCodec>())
            {
                Path = _fixture.Create<string>(),
                LogsLookupPattern = "*" + LOGFILE_EXTENSION
            }
        };

        _logCodecProcessorMock = new Mock<ILogCodecProcessor>();
        _logCodecContainerMock.Setup(x => x.FindLogCodecProcessor(_sampleProfile.Settings))
            .Returns(() => _logCodecProcessorMock.Object);
    }

    [Fact]
    public async Task LoadProfileAsync_WhenForFolder_ThenProfileLoaded_AndEventsPublished()
    {
        // Arrange
        var files = SampleFiles(_sampleProfile);

        int logsAddedHandled = 0;
        int logsRemovedHandled = 0;
        int fileRemovedHandled = 0;
        List<LogSourceBase> sourcesAdded = new();
        Profile? profileSelected = null;
        _sut.LogsAdded.Subscribe(_ => logsAddedHandled++);
        _sut.LogsRemoved.Subscribe(_ => logsRemovedHandled++);
        _sut.SourceAdded.Subscribe(sourcesAdded.Add);
        _sut.SourceRemoved.Subscribe(_ => fileRemovedHandled++);
        _sut.ProfileChanged.Subscribe(x => profileSelected = x);

        // Pre-verify
        Assert.Null(_sut.Profile);
        Assert.Equal(0, _fileWatcherFactory.InstancesCreated);

        // Act
        await _sut.LoadProfileAsync(_sampleProfile);

        // Verify
        sourcesAdded = sourcesAdded.OrderBy(x => x.Name).ToList();
        _eventBus.AssertNoEventOfType<ProfileLoadingErrorEvent>();
        Assert.Equal(_sampleProfile, _sut.Profile);
        Assert.Equal(_sampleProfile, profileSelected);
        Assert.Equal(files.Length, logsAddedHandled);
        Assert.Equal(files.Length, sourcesAdded.Count);
        Assert.Equivalent(sourcesAdded, _sut.GetSources(), strict: true);
        Assert.NotNull(_fileWatcherFactory.RecentlyCreatedInstance);
        Assert.True(_fileWatcherFactory.RecentlyCreatedInstance.IsListening);
        Assert.Equal(_sampleProfile.Settings.Source, _fileWatcherFactory.RecentlyCreatedInstance.ListeningPath);
        Assert.Equal(1, _fileWatcherFactory.InstancesCreated);
        Assert.Equal(0, logsRemovedHandled);
        Assert.Equal(0, fileRemovedHandled);
        Assert.True(_directoryMonitor.MonitoringStarted); // DirectoryMonitor is being started for folders
    }

    [Fact]
    public async Task LoadProfileAsync_WhenForFile_ThenProfileLoaded_AndDirectoryMonitorNotStarted()
    {
        // Arrange
        var file = SampleFile(_sampleProfile, true);
        ((PlainTextProfileSettings)_sampleProfile!.Settings).Path = file.FullPath;

        // Act
        await _sut.LoadProfileAsync(_sampleProfile);

        // Verify
        _eventBus.AssertNoEventOfType<ProfileLoadingErrorEvent>();
        Assert.Equal(_sampleProfile, _sut.Profile);
        Assert.Equal(1, _sut.SourcesCount);
        Assert.NotNull(_fileWatcherFactory.RecentlyCreatedInstance);
        Assert.True(_fileWatcherFactory.RecentlyCreatedInstance.IsListening);
        Assert.Equal(Path.GetDirectoryName(_sampleProfile.Settings.Source), _fileWatcherFactory.RecentlyCreatedInstance.ListeningPath);
        Assert.Equal(1, _fileWatcherFactory.InstancesCreated);
        Assert.False(_directoryMonitor.MonitoringStarted); // DirectoryMonitor is not started for files
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
        Assert.NotEmpty(_sut.GetSources());
        Assert.NotEmpty(_sut.GetLogs());
        Assert.NotEmpty(_sut.GetLoggers());
        Assert.NotEmpty(_sut.GetLogLevels());
        Assert.NotEmpty(_sut.GetThreads());
        Assert.NotNull(_fileWatcherFactory.RecentlyCreatedInstance);
        Assert.True(_fileWatcherFactory.RecentlyCreatedInstance.IsListening);

        // Act
        _sut.CloseProfile();

        // Verify
        Assert.Null(_sut.Profile);
        Assert.True(profileClosed);
        Assert.Empty(_sut.GetSources());
        Assert.Empty(_sut.GetLogs());
        Assert.Empty(_sut.GetLoggers());
        Assert.Empty(_sut.GetLogLevels());
        Assert.Empty(_sut.GetThreads());
        Assert.False(_fileWatcherFactory.RecentlyCreatedInstance.IsListening);
        Assert.Equal(1, _fileWatcherFactory.InstancesCreated);
    }

    [Fact]
    public async Task GetFiles_HappyFlowScenario()
    {
        // Arrange & Pre-verify
        var files = SampleFiles(_sampleProfile);
        Assert.Empty(_sut.GetSources());
        await _sut.LoadProfileAsync(_sampleProfile);

        // Act
        var actual = _sut.GetSources()
            .OrderBy(x => x.Name)
            .ToArray();

        // Verify
        Assert.Equal(files.Length, actual.Length);
        for (var i = 0; i < files.Length; i++)
        {
            var fileRecord = actual[i] as FileRecord;
            Assert.NotNull(fileRecord);
            Assert.Equal(files[i].FullPath, actual[i].Name);
            Assert.Equal(files[i].Result.FileArtifacts, fileRecord.Artifacts);
            Assert.Equal(files[i].Content.Length, (int)fileRecord.LastReadOffset);
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

        var addedFile = SampleFile(_sampleProfile, null);
        List<LogRecord> logsAdded = new();
        List<LogSourceBase> sourcesAdded = new();
        _sut.LogsAdded.Subscribe(logs => logsAdded.AddRange(logs));
        _sut.SourceAdded.Subscribe(sourcesAdded.Add);

        // Act
        Assert.NotNull(_fileWatcherFactory.RecentlyCreatedInstance);
        _fileWatcherFactory.RecentlyCreatedInstance.OnCreated(_sampleProfile.Settings.Source, Path.GetFileName(addedFile.FullPath));

        // Verify
        Assert.Single(sourcesAdded);
        Assert.Equal(addedFile.FullPath, sourcesAdded[0].Name);
        Assert.Contains(_sut.GetSources(), x => x.Name.Equals(addedFile.FullPath));
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
        _sut.SourceAdded.Subscribe(_ => anyFileAdded = true);
        var initialLastReadOffset = files[0].Content.Length;
        files[0] = files[0] with {
            Content = files[0].Content.Concat(_fixture.CreateMany<byte>(_fixture.Create<int>())).ToArray(),
            Result = SampleLogReadingResult(files[0].FullPath)
        };
        _fileService.AddFile(files[0].FullPath, files[0].Content);
        SetupProcessorRead(_sampleProfile, files[0], false, verifyLastReadOffset: initialLastReadOffset);
        var initialLogs = _sut.GetLogs();

        // Act
        Assert.NotNull(_fileWatcherFactory.RecentlyCreatedInstance);
        _fileWatcherFactory.RecentlyCreatedInstance.OnChanged(_sampleProfile.Settings.Source, Path.GetFileName(files[0].FullPath));

        // Verify
        Assert.False(anyFileAdded);
        var actualLogs = _sut.GetLogs();
        AssertLogRecords(files[0].Result.Records, logsAdded);
        AssertLogRecords(initialLogs.Concat(logsAdded), actualLogs);
        var actualFile = _sut.GetSources()
            .SingleOrDefault(x => x.Name.Equals(files[0].FullPath, StringComparison.OrdinalIgnoreCase))
            as FileRecord;
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
        var newFilePath = Path.Combine(_sampleProfile.Settings.Source, _fixture.Create<string>());
        _fileService.MoveFile(files[0].FullPath, newFilePath);
        files[0] = files[0] with { FullPath = newFilePath };
        LogSourceBase? sourceRenamedEventOldRecord = null, sourceRenamedEventNewRecord = null;
        _sut.SourceRenamed.Subscribe(x => (sourceRenamedEventOldRecord, sourceRenamedEventNewRecord) = (x.OldRecord, x.NewRecord));

        // Act
        Assert.NotNull(_fileWatcherFactory.RecentlyCreatedInstance);
        _fileWatcherFactory.RecentlyCreatedInstance.OnRenamed(_sampleProfile.Settings.Source, Path.GetFileName(newFilePath), Path.GetFileName(oldFilePath));

        // Verify
        Assert.NotNull(sourceRenamedEventOldRecord);
        Assert.NotNull(sourceRenamedEventNewRecord);
        Assert.Equal(oldFilePath, sourceRenamedEventOldRecord.Name);
        Assert.Equal(newFilePath, sourceRenamedEventNewRecord.Name);
        var actualFiles = _sut.GetSources();
        Assert.DoesNotContain(actualFiles, x => x.Name.Equals(oldFilePath));
        Assert.Contains(actualFiles, x => x.Name.Equals(newFilePath));
        var actualLogs = _sut.GetLogs();
        Assert.DoesNotContain(actualLogs, x => x.Source.Name.Equals(oldFilePath));
        Assert.Contains(actualLogs, x => x.Source.Name.Equals(newFilePath));
    }

    [Fact]
    public async Task FileWatcher_Removed_ThenRemovedFromLocalCollections()
    {
        // Arrange
        var files = SampleFiles(_sampleProfile);
        await _sut.LoadProfileAsync(_sampleProfile);
        var fileRemoved = files[1];
        string? sourceRemoved = null;
        _sut.SourceRemoved.Subscribe(x => sourceRemoved = x.Name);

        // Act
        Assert.NotNull(_fileWatcherFactory.RecentlyCreatedInstance);
        _fileWatcherFactory.RecentlyCreatedInstance.OnDeleted(_sampleProfile.Settings.Source, Path.GetFileName(fileRemoved.FullPath));

        // Verify
        Assert.NotNull(sourceRemoved);
        Assert.Equal(fileRemoved.FullPath, sourceRemoved);
        var actualFiles = _sut.GetSources();
        Assert.DoesNotContain(actualFiles, x => x.Name.Equals(fileRemoved.FullPath));
        var expectedFiles = new FileWithContentRecord[] { files[0] }.Concat(files[2..]).ToArray();
        Assert.Equal(actualFiles.Select(x => x.Name).Order(), expectedFiles.Select(x => x.FullPath).Order());
        var actualLogs = _sut.GetLogs();
        Assert.DoesNotContain(actualLogs, x => x.Source.Name.Equals(fileRemoved.FullPath));
    }

    [Fact]
    public async Task FileWatcher_Error_ThenHandledWithLogger()
    {
        // Arrange
        var files = SampleFiles(_sampleProfile);
        await _sut.LoadProfileAsync(_sampleProfile);
        var exception = new Exception(_fixture.Create<string>());

        // Act
        Assert.NotNull(_fileWatcherFactory.RecentlyCreatedInstance);
        _fileWatcherFactory.RecentlyCreatedInstance.OnError(exception);

        // Verify
        _logger.Logs.Any(x => x.LogLevel == LogLevel.Error
            && x.Message.Equals(exception.Message)
            && x.Exception == exception);
    }

    [Fact]
    public async Task DirectoryMonitorPulse_WhenLastReadSizeDoesNotEqualToActual_ReportsUnknownChangesDetected()
    {
        // Arrange
        AutoResetEvent autoResetEvent = new(false);
        bool unknownChangesDetectedHandled = false;
        _sut.UnknownChangesDetected.Subscribe(_ => {
            unknownChangesDetectedHandled = true;
            autoResetEvent.Set();
        });

        var files = SampleFiles(_sampleProfile);
        await _sut.LoadProfileAsync(_sampleProfile);
        var lastReadSize = _fileService.GetDirectorySize(_sampleProfile.Settings.Source, ((PlainTextProfileSettings)_sampleProfile.Settings).LogsLookupPattern, recursive: true);

        // Act scenario when lastReadSize == actual size
        _directoryMonitor.TriggerPulse(lastReadSize);
        autoResetEvent.WaitOne(FileBasedProfileLoader.UPDATE_LASTREADSIZE_WAIT_TIMEOUT_MS + 20);

        // Verify scenario when lastReadSize == actual size
        Assert.False(unknownChangesDetectedHandled);

        // Act scenario when lastReadSize != actual size
        _directoryMonitor.TriggerPulse(lastReadSize + 1);
        autoResetEvent.WaitOne(FileBasedProfileLoader.UPDATE_LASTREADSIZE_WAIT_TIMEOUT_MS + 20);

        // Verify scenario when lastReadSize != actual size
        Assert.True(unknownChangesDetectedHandled);
    }

    private static void AssertLogRecords(IEnumerable<LogRecord> expected, IEnumerable<LogRecord> actual)
    {
        var expectedOrdered = expected.OrderBy(x => x.DateTime).ToArray();
        var actualOrdered = actual.OrderBy(x => x.DateTime).ToArray();
        Assert.Equal(expectedOrdered.Length, actualOrdered.Length);
        foreach (var (expectedItem, actualItem) in expectedOrdered.Zip(actualOrdered))
        {
            Assert.Equal(expectedItem.DateTime, actualItem.DateTime);
            Assert.Equal(expectedItem.Source.Name, actualItem.Source.Name);
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

    private FileWithContentRecord SampleFile(Profile profile, bool? readFileArtifacts)
    {
        _logCodecProcessorMock.Setup(x => x.MayContainSourceArtifacts(profile.Settings)).Returns(true);

        var fileName = _fixture.Create<string>() + LOGFILE_EXTENSION;
        var sampleContent = _fixture.CreateMany<byte>(_fixture.Create<int>()).ToArray();
        var fullPath = Path.Combine(profile.Settings.Source, fileName);
        _fileService.AddFile(fullPath, sampleContent);
        var sampleResults = SampleLogReadingResult(fullPath);
        var record = new FileWithContentRecord(fullPath, sampleContent, sampleResults);
        SetupProcessorRead(profile, record, readFileArtifacts);
        return record;
    }

    private LogReadingResult SampleLogReadingResult(string fullPath)
    {
        return new LogReadingResult(_fixture.Create<SourceArtifacts>(),
            ImmutableArray.Create(_fixture.Build<LogRecord>()
                .With(x => x.Source, new FileRecord(fullPath, 0))
                .CreateMany().ToArray()),
            _fixture.CreateMany<LoggerRecord>().ToArray(),
            _fixture.CreateMany<LogLevelRecord>().ToArray(),
            []);
    }

    private void SetupProcessorRead(Profile profile, FileWithContentRecord record, bool? readSourceArtifacts, int? verifyLastReadOffset = null)
    {
        _logCodecProcessorMock.Setup(x => x.ReadAsync(profile,
                It.Is<FileRecord>(fr => fr.FullPath.Equals(record.FullPath, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Stream>(),
                It.Is<LogReadingSettings>(s => !readSourceArtifacts.HasValue || s.ReadSourceArtifacts == readSourceArtifacts)))
            .ReturnsAsync((Profile _, FileRecord fileRecord, Stream stream, LogReadingSettings _) =>
            {
                if (verifyLastReadOffset is not null)
                {
                    Assert.Equal(verifyLastReadOffset, (int)stream.Position);
                }

                stream.Read(new byte[stream.Length], 0, (int)stream.Length);
                return record.Result with {
                    Records = record.Result.Records
                        .Select(r => r with { Source = fileRecord })
                        .ToImmutableArray()
                };
            });
    }

    public void Dispose()
    {
        _sut.Dispose();
    }
}
