using System.Globalization;
using System.Reactive;
using System.Windows;
using System.Windows.Media;
using Genius.Atom.Infrastructure;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core;
using Genius.Starlog.Core.Configuration;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.UI.ValueConverters;
using Genius.Starlog.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Genius.Starlog.UI.Tests.ValueConverters;

public sealed class LogLevelToColorConverterTests : IDisposable
{
    private static readonly Color _standardColor = Colors.LightGoldenrodYellow;
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture(useMutableValueTypeGenerator: true);
    private readonly Disposer _disposer;

    public LogLevelToColorConverterTests()
    {
        _disposer = new();
        SetupServices();
    }

    public void Dispose()
    {
        _disposer.Dispose();
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
        var value = A.Fake<ILogItemViewModel>();
        A.CallTo(() => value.Record).Returns(logRecord);
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
        var optionsMock = A.Fake<IOptions<LogLevelMappingConfiguration>>();
        A.CallTo(() => optionsMock.Value).Returns(new LogLevelMappingConfiguration
        {
            TreatAsMinor = ["debug", "trace", "statistics"],
            TreatAsWarning = ["warn", "warning"],
            TreatAsError = ["err", "error", "exception"],
            TreatAsCritical = ["fatal"],
        });

        var services = new ServiceCollection();
        services.AddSingleton(optionsMock);

        var currentProfile = A.Fake<ICurrentProfile>();
        A.CallTo(() => currentProfile.ProfileClosed).Returns(A.Fake<IObservable<Unit>>());
        services.AddSingleton(currentProfile);

        var serviceProvider = services.BuildServiceProvider().DisposeWith(_disposer);

#pragma warning disable CS0618 // Type or member is obsolete
        App.OverrideServiceProvider(serviceProvider);
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
