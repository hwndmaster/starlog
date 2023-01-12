using System.Buffers;
using System.Text;
using System.Text.Json;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.Tests.Repositories;

public sealed class LogReaderJsonConverterTests
{
    private readonly Fixture _fixture = new();
    private readonly Mock<ILogReaderContainer> _logReaderContainerMock = new();
    private readonly LogReaderJsonConverter _sut;

    public LogReaderJsonConverterTests()
    {
        _sut = new LogReaderJsonConverter(_logReaderContainerMock.Object);
    }

    [Fact]
    public void Read_RestoresCompleteLogReaderObject()
    {
        // Arrange
        var value = _fixture.Create<LogReader>();
        _logReaderContainerMock.Setup(x => x.GetLogReaders()).Returns(new [] { value });
        var input = Encoding.Default.GetBytes($"\"{value.Id}\"");
        var reader = new Utf8JsonReader(input, true, new JsonReaderState());
        reader.Read();

        // Act
        var result = _sut.Read(ref reader, typeof(LogReader), new JsonSerializerOptions());

        // Verify
        Assert.Equal(value, result);
    }

    [Fact]
    public void Write_WritesOnlyId()
    {
        // Arrange
        var stream = new ArrayBufferWriter<byte>();
        var writer = new Utf8JsonWriter(stream);
        var value = _fixture.Create<LogReader>();

        // Act
        _sut.Write(writer, value, new JsonSerializerOptions());

        // Verify
        writer.Dispose();
        var jsonValue = Encoding.Default.GetString(stream.WrittenSpan);
        Assert.Equal($"\"{value.Id}\"", jsonValue);
    }
}
