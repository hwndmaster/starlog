using Genius.Atom.Infrastructure.Events;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.TestingUtil;

public sealed class ProfileHarness
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly IProfileRepository _profileRepoMock = A.Fake<IProfileRepository>();
    private readonly IProfileQueryService _profileQueryMock = A.Fake<IProfileQueryService>();
    private readonly TestCurrentProfile _currentProfile = new();
    private readonly IEventBus _eventBusMock = A.Fake<IEventBus>();

    public ProfileHarness()
    {
        A.CallTo(() => _profileQueryMock.FindByIdAsync(A<Guid>.Ignored)).Returns(default(Profile));
    }

    public Profile CreateProfile(bool setAsCurrent = false)
    {
        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            Name = Guid.NewGuid().ToString(),
            Settings = new TestProfileSettings(),
            Filters = [
                new TestProfileFilter(),
                new TestProfileFilter(),
                new TestProfileFilter(),
            ],
            MessageParsings = [
                new MessageParsing
                {
                    Name = Guid.NewGuid().ToString(),
                    Method = _fixture.Create<PatternType>(),
                    Pattern = Guid.NewGuid().ToString()
                },
                new MessageParsing
                {
                    Name = Guid.NewGuid().ToString(),
                    Method = _fixture.Create<PatternType>(),
                    Pattern = Guid.NewGuid().ToString()
                },
                new MessageParsing
                {
                    Name = Guid.NewGuid().ToString(),
                    Method = _fixture.Create<PatternType>(),
                    Pattern = Guid.NewGuid().ToString()
                }
            ]
        };

        A.CallTo(() => _profileQueryMock.FindByIdAsync(profile.Id)).Returns(profile);

        if (setAsCurrent)
        {
            _currentProfile.LoadProfileAsync(profile).GetAwaiter().GetResult();
        }
        return profile;
    }

    public void VerifyEventPublished<T>(int? numberOfTimes = null, Times? times = null)
        where T : IEventMessage
    {
        if (numberOfTimes is not null)
        {
            A.CallTo(() => _eventBusMock.Publish(A<T>.Ignored)).MustHaveHappened(numberOfTimes.Value, times ?? Times.Exactly);
        }
        else
        {
            A.CallTo(() => _eventBusMock.Publish(A<T>.Ignored)).MustHaveHappened();
        }
    }

    public IFixture Fixture => _fixture;
    public ICurrentProfile CurrentProfile => _currentProfile;
    public IProfileRepository ProfileRepo => _profileRepoMock;
    public IProfileQueryService ProfileQuery => _profileQueryMock;
    public IEventBus EventBus => _eventBusMock;
}
