using Genius.Starlog.UI.ValueConverters;

namespace Genius.Starlog.UI.Tests.ValueConverters;

public sealed class TicksToDateTimeConverterTests
{
    private const long DefaultTicks = 599292901230000000; // 01/02/1900 01:02:03.000
    private readonly TicksToDateTimeConverter _sut = new();

    [Fact]
    public void Convert_WhenStringValueProvided_ParsedAndReturned()
    {
        // Arrange
        var ticks = DefaultTicks.ToString();

        // Act
        var result = _sut.Convert(ticks, typeof(object), null!, null!) as string;

        // Verify
        Assert.Equal("1900-02-01 01:02:03.000", result);
    }

    [Fact]
    public void Convert_WhenDoubleValueProvided_ParsedAndReturned()
    {
        // Arrange
        const double ticks = DefaultTicks;

        // Act
        var result = _sut.Convert(ticks, typeof(object), null!, null!) as string;

        // Verify
        Assert.Equal("1900-02-01 01:02:03.000", result);
    }

    [Fact]
    public void Convert_WhenUnsupportedValueType_ThrowsException()
    {
        // Arrange
        var value = new object();

        // Act & Verify
        Assert.Throws<NotSupportedException>(() => _sut.Convert(value, typeof(object), null!, null!));
    }
}
