using Genius.Atom.Data.Persistence;
using Genius.Atom.Infrastructure.Entities;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Atom.Infrastructure.TestingUtil.Events;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.Tests.Repositories;

public sealed class ProfileRepositoryTests
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly TestEventBus _eventBus = new();
    private readonly Mock<IJsonPersister> _persisterMock = new();

    [Fact]
    public async Task DeleteAsync_WhenIdIsForAnonymousProfile_ThenSetsAnonymousProfileToNull()
    {
        // Arrange
        var profiles = _fixture.CreateMany<Profile>().ToArray();
        var anonProfile = _fixture.Build<Profile>().With(x => x.Id, Profile.AnonymousProfileId).Create();
        var sut = CreateSystemUnderTest(profiles);
        sut.SetAnonymous(anonProfile);

        // Pre-verify
        var preVerifyResult = await sut.FindByIdAsync(Profile.AnonymousProfileId);
        Assert.Equal(anonProfile, preVerifyResult);

        // Act
        await sut.DeleteAsync(Profile.AnonymousProfileId);

        // Verify
        var actual = await sut.FindByIdAsync(Profile.AnonymousProfileId);
        Assert.Null(actual);
        _eventBus.AssertNoEventOfType<EntitiesAffectedEvent>();
    }

    [Fact]
    public async Task DeleteAsync_WhenIdIsForProfile_ThenDeletesThatProfile()
    {
        // Arrange
        var profiles = _fixture.CreateMany<Profile>().ToArray();
        var anonProfile = _fixture.Build<Profile>().With(x => x.Id, Profile.AnonymousProfileId).Create();
        var sut = CreateSystemUnderTest(profiles);
        sut.SetAnonymous(anonProfile);
        var profileToDelete = profiles[0];

        // Pre-verify
        Assert.NotNull(await sut.FindByIdAsync(profileToDelete.Id));

        // Act
        await sut.DeleteAsync(profileToDelete.Id);

        // Verify
        var anonymousProfile = await sut.FindByIdAsync(Profile.AnonymousProfileId);
        var deletedProfile = await sut.FindByIdAsync(profileToDelete.Id);
        var actualProfiles = (await sut.GetAllAsync()).ToArray();
        Assert.NotNull(anonymousProfile);
        Assert.Null(deletedProfile);
        Assert.Equal(profiles.Skip(1), actualProfiles);
        var @event = _eventBus.GetSingleEvent<EntitiesAffectedEvent>();
        Assert.Empty(@event.Updated);
        Assert.Empty(@event.Added);
        Assert.Single(@event.Deleted);
        Assert.Equal(profileToDelete.Id, @event.Deleted.First().Key);
    }

    [Fact]
    public async Task StoreAsync_WhenContainsAnonymousProfile_ThenIgnores()
    {
        // Arrange
        var profiles = _fixture.CreateMany<Profile>().ToArray();
        var anonProfile = _fixture.Build<Profile>().With(x => x.Id, Profile.AnonymousProfileId).Create();
        var sut = CreateSystemUnderTest(profiles);
        sut.SetAnonymous(anonProfile);

        // Act
        await sut.StoreAsync(anonProfile, profiles[1]);

        // Verify
        var @event = _eventBus.GetSingleEvent<EntitiesAffectedEvent>();
        Assert.Empty(@event.Added);
        Assert.Empty(@event.Deleted);
        Assert.Single(@event.Updated);
        Assert.Equal(profiles[1].Id, @event.Updated.First().Key);
    }

    [Fact]
    public async Task StoreAsync_WhenOnlyAnonymousProfile_ThenDoesNothing()
    {
        // Arrange
        var profiles = _fixture.CreateMany<Profile>().ToArray();
        var anonProfile = _fixture.Build<Profile>().With(x => x.Id, Profile.AnonymousProfileId).Create();
        var sut = CreateSystemUnderTest(profiles);
        sut.SetAnonymous(anonProfile);

        // Act
        await sut.StoreAsync(anonProfile);

        // Verify
        _eventBus.AssertNoEventOfType<EntitiesAffectedEvent>();
    }

    private ProfileRepository CreateSystemUnderTest(Profile[] profiles)
    {
        _persisterMock.Setup(x => x.LoadCollection<Profile>(It.IsAny<string>()))
            .Returns(profiles);
        return new ProfileRepository(_eventBus, _persisterMock.Object,
            new TestLogger<ProfileRepository>(), Mock.Of<ISettingsRepository>());
    }
}
