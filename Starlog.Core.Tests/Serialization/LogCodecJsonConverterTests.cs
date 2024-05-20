using System.Buffers;
using System.Text;
using System.Text.Json;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Serialization;

namespace Genius.Starlog.Core.Tests.Serialization;

public sealed class LogCodecJsonConverterTests
{
    private readonly Fixture _fixture = new();
    private readonly ILogCodecContainer _logCodecContainerFake = A.Fake<ILogCodecContainer>();
    private readonly LogCodecJsonConverter _sut;

    public LogCodecJsonConverterTests()
    {
        _sut = new LogCodecJsonConverter(_logCodecContainerFake);
    }

    [Fact]
    public void Read_RestoresCompleteLogCodecObject()
    {
        // Arrange
        var value = _fixture.Create<LogCodec>();
        A.CallTo(() => _logCodecContainerFake.GetLogCodecs()).Returns([value]);
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
