using Genius.Starlog.UI.ValueConverters;

namespace Genius.Starlog.UI.Tests.ValueConverters;

public sealed class TickRangeToTimeConverterTests
{
    private readonly TickRangeToTimeConverter _sut = new();

    [Fact]
    public void Convert_WhenIncorrectValuesLengthProvided_ReturnsUnsetValue()
    {
        // Act 1
        var result = _sut.Convert(new object[1], typeof(object), null!, null!) as string;

        // Verify 1
        Assert.Null(result);

        // Act 2
        result = _sut.Convert(new object[3], typeof(object), null!, null!) as string;

        // Verify 2
        Assert.Null(result);
    }

    [Fact]
    public void Convert_WhenTwoValuesProvided_ReturnsRangeString()
    {
        // Arrange
        var values = new []
        {
            "800000000",
            "6000000000"
        };

        // Act
        var result = _sut.Convert(values, typeof(object), null!, null!) as string;

        // Verify
        Assert.Equal("8 min 40 sec 0 ms", result);
    }

    [Fact]
    public void Convert_WhenLessThanMinuteRangeValuesProvided_ReturnsRangeStringWithoutMinutes()
    {
        // Arrange
        var values = new []
        {
            "8000000",
            "60000000"
        };

        // Act
        var result = _sut.Convert(values, typeof(object), null!, null!) as string;

        // Verify
        Assert.Equal("5 sec 200 ms", result);
    }
}
