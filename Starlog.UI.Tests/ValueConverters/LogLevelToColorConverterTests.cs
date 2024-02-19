using System.Globalization;
using System.Reactive;
using System.Windows;
using System.Windows.Media;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core;
using Genius.Starlog.Core.Configuration;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.UI.ValueConverters;
using Genius.Starlog.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Genius.Starlog.UI.Tests.ValueConverters;

public sealed class LogLevelToColorConverterTests
{
    private static readonly Color _standardColor = Colors.LightGoldenrodYellow;
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture(useMutableValueTypeGenerator: true);

    public LogLevelToColorConverterTests()
    {
        SetupServices();
    }

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

    private void SetupServices()
    {
        var optionsMock = new Mock<IOptions<LogLevelMappingConfiguration>>();
        optionsMock.SetupGet(x => x.Value).Returns(new LogLevelMappingConfiguration
        {
            TreatAsMinor = ["debug", "trace", "statistics"],
            TreatAsWarning = ["warn", "warning"],
            TreatAsError = ["err", "error", "exception"],
            TreatAsCritical = ["fatal"],
        });

        var services = new ServiceCollection();
        services.AddSingleton(optionsMock.Object);
        services.AddSingleton(Mock.Of<ICurrentProfile>(x => x.ProfileClosed == Mock.Of<IObservable<Unit>>()));

#pragma warning disable CS0618 // Type or member is obsolete
        App.OverrideServiceProvider(services.BuildServiceProvider());
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
