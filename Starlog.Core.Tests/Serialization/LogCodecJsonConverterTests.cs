using System.Buffers;
using System.Text;
using System.Text.Json;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.Tests.Repositories;

public sealed class LogCodecJsonConverterTests
{
    private readonly Fixture _fixture = new();
    private readonly Mock<ILogCodecContainer> _logCodecContainerMock = new();
    private readonly LogCodecJsonConverter _sut;

    public LogCodecJsonConverterTests()
    {
        _sut = new LogCodecJsonConverter(_logCodecContainerMock.Object);
    }

    [Fact]
    public void Read_RestoresCompleteLogCodecObject()
    {
        // Arrange
        var value = _fixture.Create<LogCodec>();
        _logCodecContainerMock.Setup(x => x.GetLogCodecs()).Returns(new [] { value });
        var input = Encoding.Default.GetBytes($"\"{value.Id}\"");
        var reader = new Utf8JsonReader(input, true, new JsonReaderState());
        reader.Read();

        // Act
        var result = _sut.Read(ref reader, typeof(LogCodec), new JsonSerializerOptions());

        // Verify
        Assert.Equal(value, result);
    }

    [Fact]
    public void Write_WritesOnlyId()
    {
        // Arrange
        var stream = new ArrayBufferWriter<byte>();
        var writer = new Utf8JsonWriter(stream);
        var value = _fixture.Create<LogCodec>();

        // Act
        _sut.Write(writer, value, new JsonSerializerOptions());

        // Verify
        writer.Dispose();
        var jsonValue = Encoding.Default.GetString(stream.WrittenSpan);
        Assert.Equal($"\"{value.Id}\"", jsonValue);
    }
}
