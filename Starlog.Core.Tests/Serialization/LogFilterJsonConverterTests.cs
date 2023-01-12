using System.Buffers;
using System.Text;
using System.Text.Json;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.Tests.Repositories;

public sealed class LogFilterJsonConverterTests
{
    private readonly Fixture _fixture = new();
    private readonly Mock<ILogFilterContainer> _logFilterContainerMock = new();
    private readonly LogFilterJsonConverter _sut;

    public LogFilterJsonConverterTests()
    {
        _sut = new LogFilterJsonConverter(_logFilterContainerMock.Object);
    }

    [Fact]
    public void Read_RestoresCompleteLogFilterObject()
    {
        // Arrange
        var value = _fixture.Create<LogFilter>();
        _logFilterContainerMock.Setup(x => x.GetLogFilters()).Returns(new [] { value });
        var input = Encoding.Default.GetBytes($"\"{value.Id}\"");
        var reader = new Utf8JsonReader(input, true, new JsonReaderState());
        reader.Read();

        // Act
        var result = _sut.Read(ref reader, typeof(LogFilter), new JsonSerializerOptions());

        // Verify
        Assert.Equal(value, result);
    }

    [Fact]
    public void Write_WritesOnlyId()
    {
        // Arrange
        var stream = new ArrayBufferWriter<byte>();
        var writer = new Utf8JsonWriter(stream);
        var value = _fixture.Create<LogFilter>();

        // Act
        _sut.Write(writer, value, new JsonSerializerOptions());

        // Verify
        writer.Dispose();
        var jsonValue = Encoding.Default.GetString(stream.WrittenSpan);
        Assert.Equal($"\"{value.Id}\"", jsonValue);
    }
}
