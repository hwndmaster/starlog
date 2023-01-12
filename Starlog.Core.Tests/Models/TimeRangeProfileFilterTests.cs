using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Tests.Repositories;

public sealed class TimeRangeProfileFilterTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void SetTimeFromToExtended_SetsUpFromToExtendedToSecondLowerAndUpperBounds()
    {
        // Arrange
        var sut = _fixture.Create<TimeRangeProfileFilter>();
        var @from = new DateTimeOffset(1900, 1, 1, 1, 1, 1, 555, TimeSpan.Zero);
        var @to = new DateTimeOffset(1900, 1, 1, 1, 1, 2, 333, TimeSpan.Zero);

        // Act
        sut.SetTimeFromToExtended(@from, @to);

        // Verify
        Assert.Equal(new DateTimeOffset(1900, 1, 1, 1, 1, 1, 0, TimeSpan.Zero), sut.TimeFrom);
        Assert.Equal(new DateTimeOffset(1900, 1, 1, 1, 1, 2, 999, 999, TimeSpan.Zero).AddTicks(9), sut.TimeTo);
    }
}
