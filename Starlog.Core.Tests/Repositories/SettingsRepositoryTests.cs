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
    private readonly IEventBus _eventBusMock = A.Fake<IEventBus>();
    private readonly IJsonPersister _persisterMock = A.Fake<IJsonPersister>();

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
    public void Constructor_WhenNoExistingSettings_ThenDefaultLoaded()
    {
        // Arrange
        Settings? settings = null;

        // Act
        var sut = CreateSystemUnderTest(settings);

        // Verify
        var result = sut.Get();
        Assert.Null(result.AutoLoadProfile);
        Assert.False(result.AutoLoadPreviouslyOpenedProfile);
        Assert.Equal(3, result.PlainTextLogCodecLinePatterns.Count);
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
        A.CallTo(() => _persisterMock.Store(A<string>.Ignored, newSettings)).MustHaveHappened();
        A.CallTo(() => _eventBusMock.Publish(A<SettingsUpdatedEvent>.That.Matches(e => e.Settings == newSettings))).MustHaveHappenedOnceExactly();
    }

    private SettingsRepository CreateSystemUnderTest(Settings? settings = null)
    {
        A.CallTo(() => _persisterMock.Load<Settings>(A<string>.Ignored))
            .Returns(settings!);
        return new SettingsRepository(_eventBusMock, _persisterMock,
            A.Fake<ILogger<SettingsRepository>>());
    }
}
