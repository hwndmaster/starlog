using System.Text;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.LogReading.PlainTextLogCodecParsers;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.Tests.LogReading;

public sealed class PlainTextLogCodecProcessorTests
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly Mock<ISettingsQueryService> _settingsQueryMock = new();
    private readonly PlainTextLogCodecProcessor _sut;

    public PlainTextLogCodecProcessorTests()
    {
        _sut = new(_settingsQueryMock.Object, new TestLogger<PlainTextLogCodecLineMaskPatternParser>());
    }

    [Fact]
    public async Task ReadAsync_HappyFlowScenario()
    {
        // Arrange
        var profile = CreateSampleProfile();
        var fields = new LogFieldsContainer();
        var fileRecord = new FileRecord(_fixture.Create<string>(), 0);
        using var stream = new MemoryStream(Encoding.Default.GetBytes(
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
        var result = await _sut.ReadAsync(profile, fileRecord, stream, new LogReadingSettings(ReadSourceArtifacts: true), fields);

        // Verify
        Assert.Equal(new [] { (0, "thread"), (1, "logger") },
            result.UpdatedFields.GetFields().Select(x => (x.FieldId, x.FieldName)));
        Assert.NotNull(result.FileArtifacts);
        Assert.Equal(2, result.FileArtifacts.Artifacts.Length);
        Assert.Equal("FileArtifact 1", result.FileArtifacts.Artifacts[0]);
        Assert.Equal("FileArtifact 2", result.FileArtifacts.Artifacts[1]);
        Assert.Equal(2, result.LogLevels.Count);
        Assert.Equal("LEVEL1", result.LogLevels.ElementAt(0).Name);
        Assert.Equal("LEVEL2", result.LogLevels.ElementAt(1).Name);
        Assert.Equal(3, result.Records.Length);
        AssertLogRecord(result.UpdatedFields, result.Records[0],
            new DateTimeOffset(1900, 1, 1, 10, 11, 12, 444, TimeSpan.Zero),
            "LEVEL1", "Some test message", ["1", "Logger1"], null);
        AssertLogRecord(result.UpdatedFields, result.Records[1],
            new DateTimeOffset(1900, 1, 1, 10, 11, 12, 555, TimeSpan.Zero),
            "LEVEL2", "Another test message", ["2", "Logger2"], "Log artifact");
        AssertLogRecord(result.UpdatedFields, result.Records[2],
            new DateTimeOffset(1900, 1, 1, 10, 11, 12, 666, TimeSpan.Zero),
            "LEVEL1", "Yet another test message", ["2", "Logger2"], "Log artifact 1\r\nLog artifact 2");
    }

    [Fact]
    public async Task ReadAsync_WithReadFileArtifactsSetToTrue_AndWithFileArtifactLinesCountSetToZero()
    {
        // Arrange
        var profile = CreateSampleProfile();
        var fields = Mock.Of<ILogFieldsContainer>();
        var fileRecord = new FileRecord(_fixture.Create<string>(), 0);
        using var stream = new MemoryStream(Encoding.Default.GetBytes(
            """
            1900-01-01 10:11:12.444 LEVEL1 1 Logger1 Some test message
            """));
        ((PlainTextProfileSettings)profile.Settings).FileArtifactLinesCount = 0;

        // Act
        var result = await _sut.ReadAsync(profile, fileRecord, stream, new LogReadingSettings(ReadSourceArtifacts: true), fields);

        // Verify
        Assert.NotNull(result.FileArtifacts);
        Assert.Empty(result.FileArtifacts.Artifacts);
    }

    [Fact]
    public async Task ReadAsync_WithReadFileArtifactsSetToTrue_AndStreamContainsNotEnoughFileArtifacts_ReturnsNoResult()
    {
        // Arrange
        var profile = CreateSampleProfile();
        var fields = Mock.Of<ILogFieldsContainer>();
        using var stream = new MemoryStream(Encoding.Default.GetBytes(
            """
            Expected two file artifact lines, but here only single one
            """));

        // Act
        var result = await _sut.ReadAsync(profile, _fixture.Create<FileRecord>(), stream,
            new LogReadingSettings(ReadSourceArtifacts: true), fields);

        // Verify
        Assert.Equal(LogReadingResult.Empty, result);
    }

    [Fact]
    public async Task ReadAsync_WithoutReadingFileArtifacts()
    {
        // Arrange
        var profile = CreateSampleProfile();
        var fields = new LogFieldsContainer();
        var fileRecord = new FileRecord(_fixture.Create<string>(), 0);
        using var stream = new MemoryStream(Encoding.Default.GetBytes(
            """
            1900-01-01 10:11:12.444 LEVEL1 1 Logger1 Some test message
            """));

        // Act
        var result = await _sut.ReadAsync(profile, fileRecord, stream, new LogReadingSettings(ReadSourceArtifacts: false), fields);

        // Verify
        Assert.Null(result.FileArtifacts);
        Assert.Single(result.LogLevels);
        Assert.Equal("LEVEL1", result.LogLevels.ElementAt(0).Name);
        Assert.Single(result.Records);
        AssertLogRecord(result.UpdatedFields, result.Records[0],
            new DateTimeOffset(1900, 1, 1, 10, 11, 12, 444, TimeSpan.Zero),
            "LEVEL1", "Some test message", ["1", "Logger1"], null);
    }

    [Fact]
    public async Task ReadAsync_WithInvalidLine_ReturnsNoResult()
    {
        // Arrange
        var profile = CreateSampleProfile();
        var fields = Mock.Of<ILogFieldsContainer>();
        var fileRecord = new FileRecord(_fixture.Create<string>(), 0);
        using var stream = new MemoryStream(Encoding.Default.GetBytes(
            """
            invalid line
            """));

        // Act
        var result = await _sut.ReadAsync(profile, fileRecord, stream, new LogReadingSettings(ReadSourceArtifacts: false), fields);

        // Verify
        Assert.Equal(LogReadingResult.Empty, result);
    }

    [Fact]
    public async Task ReadAsync_WithInvalidLinePattern_ReturnsNoResult()
    {
        // Arrange
        var profile = CreateSampleProfile();
        var fields = Mock.Of<ILogFieldsContainer>();
        using var stream = new MemoryStream(Encoding.Default.GetBytes(_fixture.Create<string>()));
        ((PlainTextProfileSettings)profile.Settings).LinePatternId = Guid.NewGuid();

        // Act & Verify
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ReadAsync(profile, _fixture.Create<FileRecord>(), stream, new LogReadingSettings(ReadSourceArtifacts: false), fields));
    }

    [Fact]
    public async Task ReadAsync_WhenEmptyStream_ReturnsNoResult()
    {
        // Arrange
        var fields = Mock.Of<ILogFieldsContainer>();
        using var stream = new MemoryStream(Array.Empty<byte>());

        // Act
        var result = await _sut.ReadAsync(CreateSampleProfile(), _fixture.Create<FileRecord>(), stream,
            new LogReadingSettings(ReadSourceArtifacts: false), fields);

        // Verify
        Assert.Equal(LogReadingResult.Empty, result);
    }

    [Fact]
    public void ReadFromCommandLineArguments_HappyFlowScenario()
    {
        // Arrange
        var lineRegexName = _fixture.Create<string>();
        var pattern = new PatternValue {
            Name = lineRegexName,
            Type = PatternType.RegularExpression,
            Pattern = _fixture.Create<string>()
        };
        _settingsQueryMock.Setup(x => x.Get()).Returns(new Settings
        {
            PlainTextLogCodecLinePatterns = new List<PatternValue> { pattern }
        });
        var profileLogCodec = new PlainTextProfileSettings(_fixture.Create<LogCodec>())
        {
            Path = _fixture.Create<string>()
        };
        string[] codecSettings = [lineRegexName];

        // Act
        var result = _sut.ReadFromCommandLineArguments(profileLogCodec, codecSettings);

        // Verify
        Assert.True(result);
        Assert.Equal(pattern.Id, profileLogCodec.LinePatternId);
    }

    [Fact]
    public void ReadFromCommandLineArguments_WhenNoCodecSettingsProvided_ReturnsFalse()
    {
        // Arrange
        var profileLogCodec = new PlainTextProfileSettings(_fixture.Create<LogCodec>())
        {
            Path = _fixture.Create<string>()
        };
        string[] codecSettings = Array.Empty<string>();

        // Act
        var result = _sut.ReadFromCommandLineArguments(profileLogCodec, codecSettings);

        // Verify
        Assert.False(result);
    }

    [Fact]
    public void ReadFromCommandLineArguments_WhenNoTemplateFound_ReturnsFalse()
    {
        // Arrange
        var lineRegexName = _fixture.Create<string>();
        var anotherLineRegexName = _fixture.Create<string>();
        var patternId = Guid.NewGuid();
        _settingsQueryMock.Setup(x => x.Get()).Returns(new Settings
        {
            PlainTextLogCodecLinePatterns = new List<PatternValue>
            {
                new PatternValue {
                    Id = patternId,
                    Name = lineRegexName,
                    Type = PatternType.RegularExpression,
                    Pattern = _fixture.Create<string>()
                }
            }
        });
        var profileLogCodec = new PlainTextProfileSettings(_fixture.Create<LogCodec>())
        {
            Path = _fixture.Create<string>(),
            LinePatternId = patternId
        };
        string[] codecSettings = [anotherLineRegexName];

        // Act
        var result = _sut.ReadFromCommandLineArguments(profileLogCodec, codecSettings);

        // Verify
        Assert.False(result);
    }

    private static void AssertLogRecord(ILogFieldsContainer fields, LogRecord logRecord,
        DateTimeOffset dt, string level,
        string msg, string[] fieldValues, string? artifacts)
    {
        Assert.Equal(dt, logRecord.DateTime);
        Assert.Equal(level, logRecord.Level.Name);
        Assert.Equal(msg, logRecord.Message);
        Assert.Equal(artifacts, logRecord.LogArtifacts);

        Assert.Equal(fieldValues.Length, logRecord.FieldValueIndices.Length);
        for (var fieldId = 0; fieldId < fieldValues.Length; fieldId++)
        {
            var fieldValue = fields.GetFieldValue(fieldId, logRecord.FieldValueIndices[fieldId]);
            Assert.Equal(fieldValues[fieldId], fieldValue);
        }
    }

    private Profile CreateSampleProfile()
    {
        var pattern = new PatternValue {
            Id = Guid.NewGuid(),
            Name = _fixture.Create<string>(),
            Type = PatternType.RegularExpression,
            Pattern = @"(?<datetime>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}.\d{3})\s(?<level>\w+)\s(?<thread>\d+)\s(?<logger>\w+)\s(?<message>.*)"
        };

        _settingsQueryMock.Setup(x => x.Get()).Returns(new Settings
        {
            PlainTextLogCodecLinePatterns = new List<PatternValue> { pattern }
        });

        return new Profile
        {
            Name = _fixture.Create<string>(),
            Settings = new PlainTextProfileSettings(_fixture.Create<LogCodec>())
            {
                Path = _fixture.Create<string>(),
                FileArtifactLinesCount = 2,
                LinePatternId = pattern.Id,
            }
        };
    }
}
