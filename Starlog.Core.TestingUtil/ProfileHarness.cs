using Genius.Atom.Infrastructure.Events;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.TestingUtil;

public sealed class ProfileHarness
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly Mock<IProfileRepository> _profileRepoMock = new();
    private readonly Mock<IProfileQueryService> _profileQueryMock = new();
    private readonly TestCurrentProfile _currentProfile = new();
    private readonly Mock<IEventBus> _eventBusMock = new();

    public Profile CreateProfile(bool setAsCurrent = false)
    {
        var profile = _fixture.Create<Profile>();
        _profileQueryMock.Setup(x => x.FindByIdAsync(profile.Id))
            .ReturnsAsync(profile);
        if (setAsCurrent)
        {
            _currentProfile.LoadProfileAsync(profile).GetAwaiter().GetResult();
        }
        return profile;
    }

    public void VerifyEventPublished<T>(Times? times = null)
        where T : IEventMessage
    {
        _eventBusMock.Verify(x => x.Publish(It.IsAny<T>()), times ?? Times.Once());
    }

    public IFixture Fixture => _fixture;
    public ICurrentProfile CurrentProfile => _currentProfile;
    public IProfileRepository ProfileRepo => _profileRepoMock.Object;
    public IProfileQueryService ProfileQuery => _profileQueryMock.Object;
    public IEventBus EventBus => _eventBusMock.Object;
}
