using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.UI.ValueConverters;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.Tests.ValueConverters;

public sealed class LogLevelToColorConverterTests
{
    private static readonly Color _standardColor = Colors.LightGoldenrodYellow;
    private readonly IFixture _fixture = new Fixture();

    [StaFact]
    public void Convert_WhenLogLevelSeverityIsMinor_ThenCorrectColorReturned()
    {
        TestForLogLevel("debug", LogLevelToColorConverter.ColorForMinor);
        TestForLogLevel("trace", LogLevelToColorConverter.ColorForMinor);
        TestForLogLevel("statistics", LogLevelToColorConverter.ColorForMinor);
    }

    [StaFact]
    public void Convert_WhenLogLevelSeverityIsWarning_ThenCorrectColorReturned()
    {
        TestForLogLevel("warn", LogLevelToColorConverter.ColorForWarning);
        TestForLogLevel("warning", LogLevelToColorConverter.ColorForWarning);
    }

    [StaFact]
    public void Convert_WhenLogLevelSeverityIsMajor_ThenCorrectColorReturned()
    {
        TestForLogLevel("err", LogLevelToColorConverter.ColorForMajor);
        TestForLogLevel("error", LogLevelToColorConverter.ColorForMajor);
        TestForLogLevel("exception", LogLevelToColorConverter.ColorForMajor);
    }

    [StaFact]
    public void Convert_WhenLogLevelSeverityIsCritical_ThenCorrectColorReturned()
    {
        TestForLogLevel("fatal", LogLevelToColorConverter.ColorForCritical);
    }

    [StaFact]
    public void Convert_WhenLogLevelSeverityIsUndefined_ThenStandardColorReturned()
    {
        TestForLogLevel("9999", _standardColor);
    }

    private void TestForLogLevel(string logLevel, Color color)
    {
        // Arrange
        var dummy = _fixture.Create<LogRecord>();
        var logRecord = dummy with { Level = dummy.Level with { Name = logLevel } };
        var value = Mock.Of<ILogItemViewModel>(x => x.Record == logRecord);
        var sut = CreateSystemUnderTest();

        // Act
        var result = sut.Convert(value, typeof(object), null!, CultureInfo.InvariantCulture);

        // Verify
        Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(color, ((SolidColorBrush)result).Color);
    }

    private static LogLevelToColorConverter CreateSystemUnderTest()
    {
        var element = new FrameworkElement();
        element.Resources.Add("MahApps.Colors.ThemeForeground", _standardColor);

        return new(element);
    }
}
