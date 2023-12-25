using Genius.Starlog.Core.CommandHandlers;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.TestingUtil;

namespace Genius.Starlog.Core.Tests.CommandHandlers;

public sealed class ProfileFilterDeleteCommandHandlerTests
{
    private readonly ProfileHarness _harness = new();
    private readonly ProfileFilterDeleteCommandHandler _sut;

    public ProfileFilterDeleteCommandHandlerTests()
    {
        _sut = new(_harness.ProfileRepo, _harness.ProfileQuery, _harness.EventBus);
    }

    [Fact]
    public async Task Process_HappyFlowScenario()
    {
        // Arrange
        var profile = _harness.CreateProfile();
        var profileFiltersCount = profile.Filters.Count;
        var profileFilterToDelete = profile.Filters[1];
        var command = new ProfileFilterDeleteCommand(profile.Id, profileFilterToDelete.Id);

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        Mock.Get(_harness.ProfileRepo).Verify(x => x.StoreAsync(profile));
        _harness.VerifyEventPublished<ProfilesAffectedEvent>();
        Assert.Equal(profileFiltersCount - 1, profile.Filters.Count);
        Assert.DoesNotContain(profileFilterToDelete, profile.Filters);
    }

    [Fact]
    public async Task Process_WhenFilterNotFound_ThenNothingToHappen()
    {
        // Arrange
        var profile = _harness.CreateProfile();
        var profileFiltersCount = profile.Filters.Count;
        var command = new ProfileFilterDeleteCommand(profile.Id, Guid.NewGuid());

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        Mock.Get(_harness.ProfileRepo).Verify(x => x.StoreAsync(profile), Times.Never);
        _harness.VerifyEventPublished<ProfilesAffectedEvent>(Times.Never());
        Assert.Equal(profileFiltersCount, profile.Filters.Count);
    }
}
