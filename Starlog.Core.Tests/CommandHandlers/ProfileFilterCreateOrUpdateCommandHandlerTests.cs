using Genius.Starlog.Core.CommandHandlers;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.TestingUtil;

namespace Genius.Starlog.Core.Tests.CommandHandlers;

public sealed class ProfileFilterCreateOrUpdateCommandHandlerTests
{
    private readonly ProfileHarness _harness = new();
    private readonly ProfileFilterCreateOrUpdateCommandHandler _sut;

    public ProfileFilterCreateOrUpdateCommandHandlerTests()
    {
        _sut = new(_harness.ProfileRepo, _harness.ProfileQuery, _harness.EventBus);
    }

    [Fact]
    public async Task Process_WhenNewFilterProvided_ThenAddedToProfile()
    {
        // Arrange
        var profile = _harness.CreateProfile();
        var profileFiltersCount = profile.Filters.Count;
        var command = _harness.Fixture.Build<ProfileFilterCreateOrUpdateCommand>()
            .With(x => x.ProfileId, profile.Id)
            .Create();

        // Act
        var result = await _sut.ProcessAsync(command);

        // Verify
        A.CallTo(() => _harness.ProfileRepo.StoreAsync(profile)).MustHaveHappened();
        _harness.VerifyEventPublished<ProfilesAffectedEvent>();
        Assert.Equal(profileFiltersCount + 1, profile.Filters.Count);
        Assert.Equal(command.ProfileFilter, profile.Filters[^1]);
        Assert.Single(result.ProfileFiltersAdded);
        Assert.Equal(command.ProfileFilter.Id, result.ProfileFiltersAdded[0]);
        Assert.Empty(result.ProfileFiltersUpdated);
    }

    [Fact]
    public async Task Process_WhenExistingFilterProvided_ThenUpdatedAndPersisted()
    {
        // Arrange
        var profile = _harness.CreateProfile();
        var profileFiltersCount = profile.Filters.Count;
        var updatingProfileFilterId = _harness.Fixture.Create<Guid>();
        profile.Filters[0] = new TestProfileFilter(updatingProfileFilterId);
        var command = new ProfileFilterCreateOrUpdateCommand
        {
            ProfileId = profile.Id,
            ProfileFilter = new TestProfileFilter(updatingProfileFilterId)
        };

        // Act
        var result = await _sut.ProcessAsync(command);

        // Verify
        A.CallTo(() => _harness.ProfileRepo.StoreAsync(profile)).MustHaveHappened();
        _harness.VerifyEventPublished<ProfilesAffectedEvent>();
        Assert.Equal(profileFiltersCount, profile.Filters.Count);
        Assert.Equal(command.ProfileFilter, profile.Filters[0]);
        Assert.Empty(result.ProfileFiltersAdded);
        Assert.Single(result.ProfileFiltersUpdated);
        Assert.Equal(command.ProfileFilter.Id, result.ProfileFiltersUpdated[0]);
    }
}
