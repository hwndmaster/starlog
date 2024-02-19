using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Tests.LogFiltering;

public sealed class LogFilterContainerTests
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture(useMutableValueTypeGenerator: true);

    [Fact]
    public void CreateProfileFilter_ForProfileFilterType_CreatesInstance()
    {
        // Arrange
        var sut = CreateSystemUnderTest();
        var logFilters = RegisterLogFilters(sut);

        // Act
        var actual1 = sut.CreateProfileFilter<SampleProfileFilter1>();
        var actual3 = sut.CreateProfileFilter<SampleProfileFilter3>();

        // Verify
        Assert.NotNull(actual1);
        Assert.NotNull(actual3);
        Assert.Equal(logFilters[0], actual1.LogFilter);
        Assert.Equal(logFilters[0].Name, actual1.Name);
        Assert.Equal(logFilters[2], actual3.LogFilter);
        Assert.Equal(logFilters[2].Name, actual3.Name);
    }

    [Fact]
    public void CreateProfileFilter_ForProfileFilterType_WhenNameSpecified_CreatesInstanceWithName()
    {
        // Arrange
        var sut = CreateSystemUnderTest();
        RegisterLogFilters(sut);
        var name = _fixture.Create<string>();

        // Act
        var actual = sut.CreateProfileFilter<SampleProfileFilter2>(name);

        // Verify
        Assert.NotNull(actual);
        Assert.Equal(name, actual.Name);
    }

    [Fact]
    public void CreateProfileFilter_ForLogFilter_CreatesInstance()
    {
        // Arrange
        var sut = CreateSystemUnderTest();
        var logFilters = RegisterLogFilters(sut);

        // Act
        var actual = sut.CreateProfileFilter(logFilters[1]);

        // Verify
        Assert.True(actual is SampleProfileFilter2);
        Assert.Equal(logFilters[1], actual.LogFilter);
        Assert.Equal(logFilters[1].Name, actual.Name);
    }

    [Fact]
    public void CreateProfileFilter_ForLogFilter_WhenRegistrationNotFound_ThrowsException()
    {
        // Arrange
        var sut = CreateSystemUnderTest();
        var logFilters = RegisterLogFilters(sut);

        // Act & Verify
        Assert.Throws<InvalidOperationException>(() => sut.CreateProfileFilter(_fixture.Create<LogFilter>()));
    }

    [Fact]
    public void GetFilterProcessor_HappyFlowScenario()
    {
        // Arrange
        var sut = CreateSystemUnderTest();
        var logFilters = RegisterLogFilters(sut);
        var profileFilter1 = new SampleProfileFilter1(logFilters[0]);
        var profileFilter2 = new SampleProfileFilter1(logFilters[1]);
        var profileFilter3 = new SampleProfileFilter1(logFilters[2]);

        // Act
        var processor1 = sut.GetFilterProcessor(profileFilter1);
        var processor2 = sut.GetFilterProcessor(profileFilter2);
        var processor3 = sut.GetFilterProcessor(profileFilter3);

        // Verify
        Assert.True(processor1 is SampleFilterProcessor1);
        Assert.True(processor2 is SampleFilterProcessor2);
        Assert.True(processor3 is SampleFilterProcessor3);
    }

    [Fact]
    public void GetFilterProcessor_WhenRegistrationNotFound_ThrowsException()
    {
        // Arrange
        var sut = CreateSystemUnderTest();
        var logFilters = RegisterLogFilters(sut);
        var profileFilter = new SampleProfileFilter1(_fixture.Create<LogFilter>());

        // Act & Verify
        Assert.Throws<InvalidOperationException>(() => sut.GetFilterProcessor(profileFilter));
    }

    [Fact]
    public void RegisterLogFilter_AndGetLogFilters_HappyFlowScenario()
    {
        // Arrange
        var sut = CreateSystemUnderTest();

        // Pre-verify
        Assert.Empty(sut.GetLogFilters());

        // Act
        var logFilters = RegisterLogFilters(sut);
        var actual = sut.GetLogFilters();

        // Verify
        Assert.Equal(logFilters, actual);
    }

    [Fact]
    public void RegisterLogFilter_WhenRegistrationExists_ThrowsException()
    {
        // Arrange
        var sut = CreateSystemUnderTest();
        var logFilters = RegisterLogFilters(sut);

        // Act & Verify
        Assert.Throws<InvalidOperationException>(() => sut.RegisterLogFilter<SampleProfileFilter1, SampleFilterProcessor1>(logFilters[0]));
    }

    private static LogFilterContainer CreateSystemUnderTest()
    {
        var filterProcessors = new IFilterProcessor[]
        {
            new SampleFilterProcessor1(),
            new SampleFilterProcessor2(),
            new SampleFilterProcessor3()
        };
        return new LogFilterContainer(filterProcessors);
    }

    private LogFilter[] RegisterLogFilters(LogFilterContainer sut)
    {
        var logFilters = _fixture.CreateMany<LogFilter>(3).ToArray();
        sut.RegisterLogFilter<SampleProfileFilter1, SampleFilterProcessor1>(logFilters[0]);
        sut.RegisterLogFilter<SampleProfileFilter2, SampleFilterProcessor2>(logFilters[1]);
        sut.RegisterLogFilter<SampleProfileFilter3, SampleFilterProcessor3>(logFilters[2]);

        return logFilters;
    }

    private class SampleProfileFilter1 : ProfileFilterBase
    {
        public SampleProfileFilter1(LogFilter logFilter) : base(logFilter) { }
    }

    private class SampleProfileFilter2 : ProfileFilterBase
    {
        public SampleProfileFilter2(LogFilter logFilter) : base(logFilter) { }
    }

    private class SampleProfileFilter3 : ProfileFilterBase
    {
        public SampleProfileFilter3(LogFilter logFilter) : base(logFilter) { }
    }

    private class SampleFilterProcessor1 : IFilterProcessor
    {
        public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log) => throw new NotImplementedException();
    }

    private class SampleFilterProcessor2 : IFilterProcessor
    {
        public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log) => throw new NotImplementedException();
    }

    private class SampleFilterProcessor3 : IFilterProcessor
    {
        public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log) => throw new NotImplementedException();
    }
}
