using System.Text;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.Tests.LogReading;

public sealed class PlainTextProfileLogCodecProcessorTests
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly Mock<ISettingsQueryService> _settingsQueryMock = new();
    private readonly PlainTextLogCodecProcessor _sut;

    public PlainTextProfileLogCodecProcessorTests()
    {
        _sut = new(_settingsQueryMock.Object);
    }

    [Fact]
    public async Task ReadAsync_HappyFlowScenario()
    {
        // Arrange
        var profile = CreateSampleProfile();
        var fileRecord = new FileRecord(_fixture.Create<string>(), 0);
        var stream = new MemoryStream(Encoding.Default.GetBytes(
            """
            FileArtifact 1
            FileArtifact 2
            1900-01-01 10:11:12.444 LEVEL1 1 Logger1 Some test message
            1900-01-01 10:11:12.555 LEVEL2 2 Logger2 Another test message
            Log artifact
            1900-01-01 10:11:12.666 LEVEL1 2 Logger2 Yet another test message
            Log artifact 1
            Log artifact 2
            """));

        // Act
        var result = await _sut.ReadAsync(profile, fileRecord, stream, new LogReadingSettings(ReadFileArtifacts: true));

        // Verify
        Assert.NotNull(result.FileArtifacts);
        Assert.Equal(2, result.FileArtifacts.Artifacts.Length);
        Assert.Equal("FileArtifact 1", result.FileArtifacts.Artifacts[0]);
        Assert.Equal("FileArtifact 2", result.FileArtifacts.Artifacts[1]);
        Assert.Equal(2, result.Loggers.Count);
        Assert.Equal("Logger1", result.Loggers.ElementAt(0).Name);
        Assert.Equal("Logger2", result.Loggers.ElementAt(1).Name);
        Assert.Equal(2, result.LogLevels.Count);
        Assert.Equal("LEVEL1", result.LogLevels.ElementAt(0).Name);
        Assert.Equal("LEVEL2", result.LogLevels.ElementAt(1).Name);
        Assert.Equal(3, result.Records.Length);
        AssertLogRecord(result.Records[0],
            new DateTimeOffset(1900, 1, 1, 10, 11, 12, 444, TimeSpan.Zero),
            "LEVEL1", "1", "Logger1", "Some test message", null);
        AssertLogRecord(result.Records[1],
            new DateTimeOffset(1900, 1, 1, 10, 11, 12, 555, TimeSpan.Zero),
            "LEVEL2", "2", "Logger2", "Another test message", "Log artifact");
        AssertLogRecord(result.Records[2],
            new DateTimeOffset(1900, 1, 1, 10, 11, 12, 666, TimeSpan.Zero),
            "LEVEL1", "2", "Logger2", "Yet another test message", "Log artifact 1\r\nLog artifact 2");
    }

    [Fact]
    public async Task ReadAsync_WithoutReadingFileArtifacts()
    {
        // Arrange
        var profile = CreateSampleProfile();
        var fileRecord = new FileRecord(_fixture.Create<string>(), 0);
        var stream = new MemoryStream(Encoding.Default.GetBytes(
            """
            1900-01-01 10:11:12.444 LEVEL1 1 Logger1 Some test message
            """));

        // Act
        var result = await _sut.ReadAsync(profile, fileRecord, stream, new LogReadingSettings(ReadFileArtifacts: false));

        // Verify
        Assert.Null(result.FileArtifacts);
        Assert.Equal(1, result.Loggers.Count);
        Assert.Equal("Logger1", result.Loggers.ElementAt(0).Name);
        Assert.Equal(1, result.LogLevels.Count);
        Assert.Equal("LEVEL1", result.LogLevels.ElementAt(0).Name);
        Assert.Single(result.Records);
        AssertLogRecord(result.Records[0],
            new DateTimeOffset(1900, 1, 1, 10, 11, 12, 444, TimeSpan.Zero),
            "LEVEL1", "1", "Logger1", "Some test message", null);
    }

    private static void AssertLogRecord(LogRecord logRecord, DateTimeOffset dt, string level, string thread,
        string logger, string msg, string? artifacts)
    {
        Assert.Equal(dt, logRecord.DateTime);
        Assert.Equal(level, logRecord.Level.Name);
        Assert.Equal(thread, logRecord.Thread);
        Assert.Equal(logger, logRecord.Logger.Name);
        Assert.Equal(msg, logRecord.Message);
        Assert.Equal(artifacts, logRecord.LogArtifacts);
    }

    private Profile CreateSampleProfile()
    {
        return new Profile
        {
            Name = _fixture.Create<string>(),
            Path = _fixture.Create<string>(),
            Settings = new ProfileSettings
            {
                FileArtifactLinesCount = 2,
                LogCodec = new PlainTextProfileLogCodec(_fixture.Create<LogCodec>())
                {
                    LineRegex = @"(?<datetime>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}.\d{3})\s(?<level>\w+)\s(?<thread>\d+)\s(?<logger>\w+)\s(?<message>.*)"
                }
            }
        };
    }
}
