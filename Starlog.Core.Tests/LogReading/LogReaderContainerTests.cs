using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Tests.LogReading;

public sealed class LogReaderContainerTests
{
    [Fact]
    public void CreateProfileLogReader_WhenNoReaderAvailable_ThrowsException()
    {
        // Arrange
        var logReader = new LogReader(Guid.NewGuid(), Guid.NewGuid().ToString());
        var sut = new LogReaderContainer(new [] { new DummyLogReaderProcessor() });

        // Act & Verify
        Assert.Throws<InvalidOperationException>(() => sut.CreateProfileLogReader(logReader));
    }

    [Fact]
    public void CreateProfileLogReader_ReturnsProfileFilterForMatchingLogReader()
    {
        // Arrange
        var sut = new LogReaderContainer(new [] { new DummyLogReaderProcessor() });
        var logReader = new LogReader(Guid.NewGuid(), Guid.NewGuid().ToString());
        sut.RegisterLogReader<DummyProfileLogRead, DummyLogReaderProcessor>(logReader);

        // Act
        var result = sut.CreateProfileLogReader(logReader);

        // Verify
        Assert.NotNull(result);
        Assert.IsType<DummyProfileLogRead>(result);
        Assert.Equal(logReader, result.LogReader);
    }

    [Fact]
    public void CreateLogReaderProcessor_WhenNoReaderAvailable_ThrowsException()
    {
        // Arrange
        var sut = new LogReaderContainer(new [] { new DummyLogReaderProcessor() });

        // Act & Verify
        Assert.Throws<InvalidOperationException>(() => sut.CreateLogReaderProcessor(new DummyProfileLogRead()));
    }

    [Fact]
    public void CreateLogReaderProcessor_ReturnsProcessorForMatchingFilterType()
    {
        // Arrange
        var sut = new LogReaderContainer(new [] { new DummyLogReaderProcessor() });
        var profileFilter = new DummyProfileLogRead();
        sut.RegisterLogReader<DummyProfileLogRead, DummyLogReaderProcessor>(profileFilter.LogReader);

        // Act
        var result = sut.CreateLogReaderProcessor(profileFilter);

        // Verify
        Assert.NotNull(result);
        Assert.IsType<DummyLogReaderProcessor>(result);
    }

    [Fact]
    public void RegisterLogReader_ForDuplicatedLogReader_ThrowsException()
    {
        // Arrange
        var sut = new LogReaderContainer(new [] { new DummyLogReaderProcessor() });
        var logReader = new LogReader(Guid.NewGuid(), Guid.NewGuid().ToString());
        sut.RegisterLogReader<DummyProfileLogRead, DummyLogReaderProcessor>(logReader);

        // Act
        Assert.Throws<InvalidOperationException>(() => sut.RegisterLogReader<DummyProfileLogRead, DummyLogReaderProcessor>(logReader));
    }

    [Fact]
    public void GetLogReaders_ReturnsCurrentlyRegisteredReaders()
    {
        // Arrange
        var sut = new LogReaderContainer(new [] { new DummyLogReaderProcessor() });
        var logReader1 = new LogReader(Guid.NewGuid(), Guid.NewGuid().ToString());
        var logReader2 = new LogReader(Guid.NewGuid(), Guid.NewGuid().ToString());
        sut.RegisterLogReader<DummyProfileLogRead, DummyLogReaderProcessor>(logReader1);
        sut.RegisterLogReader<DummyProfileLogRead, DummyLogReaderProcessor>(logReader2);

        // Act
        var result = sut.GetLogReaders().ToList();

        // Verify
        Assert.Equal(2, result.Count);
        Assert.Equal(logReader1, result[0]);
        Assert.Equal(logReader2, result[1]);
    }

    private class DummyLogReaderProcessor : ILogReaderProcessor
    {
        public Task<LogReaderResult> ReadAsync(Profile profile, FileRecord fileRecord, Stream stream, bool readFileArtifacts)
        {
            throw new NotImplementedException();
        }
    }

    private class DummyProfileLogRead : ProfileLogReadBase
    {
        public DummyProfileLogRead() : base(new LogReader(Guid.NewGuid(), Guid.NewGuid().ToString()))
        {
        }

        public DummyProfileLogRead(LogReader logReader) : base(logReader)
        {
        }
    }
}
