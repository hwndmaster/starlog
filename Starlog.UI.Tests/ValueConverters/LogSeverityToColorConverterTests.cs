using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.ValueConverters;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.Core.Tests.CommandHandlers;

public sealed class LogSeverityToColorConverterTests
{
    private static readonly Color _standardColor = Colors.LightGoldenrodYellow;
    private readonly IFixture _fixture = new Fixture();

    [StaFact]
    public void Convert_WhenLogLevelSeverityIsMinor_ThenCorrectColorReturned()
    {
        TestForSeverity(LogSeverity.Minor, LogSeverityToColorConverter.ColorForMinor);
    }

    [StaFact]
    public void Convert_WhenLogLevelSeverityIsWarning_ThenCorrectColorReturned()
    {
        TestForSeverity(LogSeverity.Warning, LogSeverityToColorConverter.ColorForWarning);
    }

    [StaFact]
    public void Convert_WhenLogLevelSeverityIsMajor_ThenCorrectColorReturned()
    {
        TestForSeverity(LogSeverity.Major, LogSeverityToColorConverter.ColorForMajor);
    }

    [StaFact]
    public void Convert_WhenLogLevelSeverityIsCritical_ThenCorrectColorReturned()
    {
        TestForSeverity(LogSeverity.Critical, LogSeverityToColorConverter.ColorForCritical);
    }

    [StaFact]
    public void Convert_WhenLogLevelSeverityIsUndefined_ThenStandardColorReturned()
    {
        TestForSeverity((LogSeverity)9999, _standardColor);
    }

    private void TestForSeverity(LogSeverity severity, Color color)
    {
        // Arrange
        var dummy = _fixture.Create<LogFlow.LogRecord>();
        var logRecord = dummy with { Level = dummy.Level with { Severity = severity } };
        var value = Mock.Of<ILogItemViewModel>(x => x.Record == logRecord);
        var sut = CreateSystemUnderTest();

        // Act
        var result = sut.Convert(value, typeof(object), null!, CultureInfo.InvariantCulture);

        // Verify
        Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(color, ((SolidColorBrush)result).Color);
    }

    private static LogSeverityToColorConverter CreateSystemUnderTest()
    {
        var element = new FrameworkElement();
        element.Resources.Add("MahApps.Colors.ThemeForeground", _standardColor);

        return new(element);
    }
}
