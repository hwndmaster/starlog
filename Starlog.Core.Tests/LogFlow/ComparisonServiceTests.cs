using System.Collections.Immutable;
using Genius.Atom.Infrastructure;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Tests.LogFlow;

public sealed class ComparisonServiceTests
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly Mock<IProfileLoader> _profileLoaderMock = new();
    private readonly ComparisonService _sut;

    public ComparisonServiceTests()
    {
        _sut = new(_profileLoaderMock.Object);
    }

    /// <summary>
    ///   A test scenario which combines 4x4 records in the following order:
    ///   1       1
    ///   2
    ///           2
    ///           3
    ///   3
    ///   4       4
    /// </summary>
    [Fact]
    public async Task LoadProfilesAsync_HappyFlowScenario()
    {
        // Arrange
        var dt = new TestDateTime();
        var logLevel1 = _fixture.Create<LogLevelRecord>();
        var logLevel2 = _fixture.Create<LogLevelRecord>();
        var file1AtProfile1 = new FileRecord(_fixture.Create<string>() + @"\file1.log", _fixture.Create<long>());
        var file2AtProfile1 = new FileRecord(_fixture.Create<string>() + @"\file2.log", _fixture.Create<long>());
        var file1AtProfile2 = new FileRecord(_fixture.Create<string>() + @"\file1.log", _fixture.Create<long>());
        var file2AtProfile2 = new FileRecord(_fixture.Create<string>() + @"\file2.log", _fixture.Create<long>());
        var logger1 = _fixture.Create<LoggerRecord>();
        var logger2 = _fixture.Create<LoggerRecord>();
        var message1 = _fixture.Create<string>();
        var message2a = _fixture.Create<string>();
        var message2b = _fixture.Create<string>();
        var message3a = _fixture.Create<string>();
        var message3b = _fixture.Create<string>();
        var message4 = _fixture.Create<string>();
        var profile1 = CreateSampleProfile(new [] {
            new LogRecord(dt.NowOffsetUtc, logLevel1, _fixture.Create<string>(), file2AtProfile1, logger1, message1, null),
            new LogRecord(dt.NowOffsetUtc.AddSeconds(1.23), logLevel1, _fixture.Create<string>(), file1AtProfile1, logger1, message2a, null),
            new LogRecord(dt.NowOffsetUtc.AddSeconds(3.76), logLevel1, _fixture.Create<string>(), file1AtProfile1, logger1, message3a, null),
            new LogRecord(dt.NowOffsetUtc.AddSeconds(4.65), logLevel2, _fixture.Create<string>(), file1AtProfile1, logger2, message4, null)
        });
        var profile2 = CreateSampleProfile(new [] {
            new LogRecord(dt.NowOffsetUtc.AddSeconds(100.12), logLevel1, _fixture.Create<string>(), file2AtProfile2, logger1, message1, null),
            new LogRecord(dt.NowOffsetUtc.AddSeconds(101.87), logLevel1, _fixture.Create<string>(), file1AtProfile2, logger1, message2b, null),
            new LogRecord(dt.NowOffsetUtc.AddSeconds(102.34), logLevel1, _fixture.Create<string>(), file1AtProfile2, logger1, message3b, null),
            new LogRecord(dt.NowOffsetUtc.AddSeconds(104.45), logLevel2, _fixture.Create<string>(), file1AtProfile2, logger2, message4, null)
        });

        // Act
        var result = await _sut.LoadProfilesAsync(profile1, profile2);

        // Verify
        // TODO ...
    }

    private Profile CreateSampleProfile(IEnumerable<LogRecord> logRecords)
    {
        var profile = new Profile
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

        _profileLoaderMock.Setup(x => x.LoadProfileAsync(profile, It.IsAny<ILogContainerWriter>()))
            .Callback((Profile profile, ILogContainerWriter logContainerWriter) =>
            {
                logContainerWriter.AddLogs(logRecords.ToImmutableArray());
            })
            .Returns(Task.FromResult(true));

        return profile;
    }
}
