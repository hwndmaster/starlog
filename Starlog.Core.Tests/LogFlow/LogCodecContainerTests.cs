using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Tests.LogFlow;

public sealed class LogCodecContainerTests
{
    [Fact]
    public void CreateProfileLogCodec_WhenNoCodecAvailable_ThrowsException()
    {
        // Arrange
        var logCodec = new LogCodec(Guid.NewGuid(), Guid.NewGuid().ToString());
        var sut = CreateSystemUnderTest();

        // Act & Verify
        Assert.Throws<InvalidOperationException>(() => sut.CreateProfileSettings(logCodec));
    }

    [Fact]
    public void CreateProfileLogCodec_ReturnsProfileFilterForMatchingLogCodec()
    {
        // Arrange
        var sut = CreateSystemUnderTest();
        var logCodec = new LogCodec(Guid.NewGuid(), Guid.NewGuid().ToString());
        sut.RegisterLogCodec<DummyProfileLogRead, DummyLogReadProcessor>(logCodec);

        // Act
        var result = sut.CreateProfileSettings(logCodec);

        // Verify
        Assert.NotNull(result);
        Assert.IsType<DummyProfileLogRead>(result);
        Assert.Equal(logCodec, result.LogCodec);
    }

    [Fact]
    public void FindLogCodecProcessor_WhenNoCodecAvailable_ThrowsException()
    {
        // Arrange
        var sut = CreateSystemUnderTest();

        // Act & Verify
        Assert.Throws<InvalidOperationException>(() => sut.FindLogCodecProcessor(new DummyProfileLogRead()));
    }

    [Fact]
    public void FindLogCodecProcessor_ReturnsProcessorForMatchingFilterType()
    {
        // Arrange
        var sut = CreateSystemUnderTest();
        var profileFilter = new DummyProfileLogRead();
        sut.RegisterLogCodec<DummyProfileLogRead, DummyLogReadProcessor>(profileFilter.LogCodec);

        // Act
        var result = sut.FindLogCodecProcessor(profileFilter);

        // Verify
        Assert.NotNull(result);
        Assert.IsType<DummyLogReadProcessor>(result);
    }

    [Fact]
    public void RegisterLogCodec_ForDuplicatedLogCodec_ThrowsException()
    {
        // Arrange
        var sut = CreateSystemUnderTest();
        var logCodec = new LogCodec(Guid.NewGuid(), Guid.NewGuid().ToString());
        sut.RegisterLogCodec<DummyProfileLogRead, DummyLogReadProcessor>(logCodec);

        // Act
        Assert.Throws<InvalidOperationException>(() => sut.RegisterLogCodec<DummyProfileLogRead, DummyLogReadProcessor>(logCodec));
    }

    [Fact]
    public void GetLogCodecs_ReturnsCurrentlyRegisteredCodecs()
    {
        // Arrange
        var sut = CreateSystemUnderTest();
        var logCodec1 = new LogCodec(Guid.NewGuid(), Guid.NewGuid().ToString());
        var logCodec2 = new LogCodec(Guid.NewGuid(), Guid.NewGuid().ToString());
        sut.RegisterLogCodec<DummyProfileLogRead, DummyLogReadProcessor>(logCodec1);
        sut.RegisterLogCodec<DummyProfileLogRead, DummyLogReadProcessor>(logCodec2);

        // Act
        var result = sut.GetLogCodecs().ToList();

        // Verify
        Assert.Equal(2, result.Count);
        Assert.Equal(logCodec1, result[0]);
        Assert.Equal(logCodec2, result[1]);
    }

    private static LogCodecContainer CreateSystemUnderTest()
    {
        return new LogCodecContainer(new Lazy<IEnumerable<ILogCodecProcessor>>(() => new[] { new DummyLogReadProcessor() }));
    }

    private class DummyLogReadProcessor : ILogCodecProcessor
    {
        public Task<LogReadingResult> ReadAsync(Profile profile, LogSourceBase source, Stream stream, LogReadingSettings settings, ILogFieldsContainer fields)
            => throw new NotImplementedException();

        public bool ReadFromCommandLineArguments(ProfileSettingsBase profileSettings, string[]? codecSettings)
            => throw new NotImplementedException();

        public bool MayContainSourceArtifacts(ProfileSettingsBase profileSettings)
            => false;
    }

    private class DummyProfileLogRead : ProfileSettingsBase
    {
        public DummyProfileLogRead() : base(new LogCodec(Guid.NewGuid(), Guid.NewGuid().ToString())) { }
        public DummyProfileLogRead(LogCodec logCodec) : base(logCodec) { }

        public override string Source => throw new NotImplementedException();

        internal override ProfileSettingsBase CloneInternal()
        {
            throw new NotImplementedException();
        }
    }
}
