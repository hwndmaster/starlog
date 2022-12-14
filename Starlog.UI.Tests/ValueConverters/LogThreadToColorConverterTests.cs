using System.Windows.Media;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.UI.ValueConverters;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.Tests.ValueConverters;

public sealed class LogThreadToColorConverterTests
{
    [Fact]
    public void Convert_ForSeveralThreads_ReturnsUniqueColors()
    {
        // Arrange
        var sut = new LogThreadToColorConverter();
        var records = Enumerable.Range(1, 25)
            .Select(x => new LogRecord() with { Thread = x.ToString() });
        var vms = records.Select(record => Mock.Of<ILogItemViewModel>(x => x.Record == record)).ToList();

        // Act
        var results = vms.Select(vm => sut.Convert(vm, typeof(object), null!, null!));

        // Verify
        var distinctColors = results.Cast<SolidColorBrush>().Select(x => x.Color.ToString()).Distinct();

        Assert.Equal(vms.Count, distinctColors.Count());
    }

    [Fact]
    public void Convert_ForSameThreads_ReturnsMatchingColors()
    {
        // Arrange
        var sut = new LogThreadToColorConverter();
        var records = new []
        {
            new LogRecord() with { Thread = "1" },
            new LogRecord() with { Thread = "2" },
            new LogRecord() with { Thread = "2" },
            new LogRecord() with { Thread = "3" }
        };
        var vms = records.Select(record => Mock.Of<ILogItemViewModel>(x => x.Record == record)).ToList();

        // Act
        var results = vms.Select(vm => sut.Convert(vm, typeof(object), null!, null!));

        // Verify
        var castedResults = results.Cast<SolidColorBrush>().ToList();
        Assert.NotEqual(castedResults[0].Color, castedResults[1].Color);
        Assert.Equal(castedResults[1].Color, castedResults[2].Color);
        Assert.NotEqual(castedResults[2].Color, castedResults[3].Color);
        Assert.NotEqual(castedResults[0].Color, castedResults[3].Color);
    }

    [Fact]
    public void Convert_WhenViewModelIsOfIncorrectType_ThrowsException()
    {
        // Arrange
        var sut = new LogThreadToColorConverter();

        // Act & Verify
        Assert.Throws<InvalidOperationException>(() => sut.Convert(new object(), typeof(object), null!, null!));
    }
}
