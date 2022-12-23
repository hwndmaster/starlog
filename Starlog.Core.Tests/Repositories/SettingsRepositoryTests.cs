using Genius.Atom.Data.Persistence;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core.Tests.Repositories;

public sealed class SettingsRepositoryTests
{
    private readonly Fixture _fixture = new();
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly Mock<IJsonPersister> _persisterMock = new();

    [Fact]
    public void Constructor_WhenExistingSettings_ThenLoaded()
    {
        // Arrange
        var settings = _fixture.Create<Settings>();

        // Act
        var sut = CreateSystemUnderTest(settings);

        // Verify
        Assert.Equal(settings, sut.Get());
    }

    [Fact]
    public void Constructor__WhenNoExistingSettings_ThenDefaultLoaded()
    {
        // Arrange
        Settings? settings = null;

        // Act
        var sut = CreateSystemUnderTest(settings);

        // Verify
        var result = sut.Get();
        Assert.Null(result.AutoLoadProfile);
        Assert.False(result.AutoLoadPreviouslyOpenedProfile);
        Assert.Equal(1, result.PlainTextLogReaderLineRegexes.Count);
    }

    [Fact]
    public void Get_ReturnsCurrentlyLoadedSettings()
    {
        // Arrange
        var settings = _fixture.Create<Settings>();
        var sut = CreateSystemUnderTest(settings);

        // Act
        var result = sut.Get();

        // Verify
        Assert.Equal(settings, result);
    }

    [Fact]
    public void Store_WhenArgumentNotProvided_ThrowsException()
    {
        // Arrange
        var sut = CreateSystemUnderTest();

        // Act & Verify
        Assert.Throws<ArgumentNullException>(() => sut.Store(null!));
    }

    [Fact]
    public void Store_ReplacesExistingSettings_AndUpdatesCache_AndFiresEvent()
    {
        // Arrange
        var sut = CreateSystemUnderTest();
        var newSettings = _fixture.Create<Settings>();

        // Act
        sut.Store(newSettings);

        // Verify
        Assert.Equal(newSettings, sut.Get());
        _persisterMock.Verify(x => x.Store(It.IsAny<string>(), newSettings));
        _eventBusMock.Verify(x => x.Publish(It.Is<SettingsUpdatedEvent>(e => e.Settings == newSettings)), Times.Once);
    }

    private SettingsRepository CreateSystemUnderTest(Settings? settings = null)
    {
        _persisterMock.Setup(x => x.Load<Settings>(It.IsAny<string>()))
            .Returns(settings!);
        return new SettingsRepository(_eventBusMock.Object, _persisterMock.Object,
            Mock.Of<ILogger<SettingsRepository>>());
    }
}
