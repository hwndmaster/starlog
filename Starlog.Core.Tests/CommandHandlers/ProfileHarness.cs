using Genius.Atom.Infrastructure.Events;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.Tests.CommandHandlers;

public sealed class ProfileHarness
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly Mock<IProfileRepository> _profileRepoMock = new();
    private readonly Mock<IProfileQueryService> _profileQueryMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();

    public ProfileHarness()
    {
        _fixture.Customize(new AutoMoqCustomization());
    }

    public T Create<T>()
    {
        return _fixture.Create<T>();
    }

    public Profile CreateProfile()
    {
        var profile = _fixture.Create<Profile>();
        _profileQueryMock.Setup(x => x.FindByIdAsync(profile.Id))
            .ReturnsAsync(profile);
        return profile;
    }

    public void VerifyEventPublished<T>(Times? times = null)
        where T : IEventMessage
    {
        _eventBusMock.Verify(x => x.Publish(It.IsAny<T>()), times ?? Times.Once());
    }

    internal AutoFixture.Dsl.ICustomizationComposer<T> Build<T>()
    {
        return _fixture.Build<T>();
    }

    public IProfileRepository ProfileRepo => _profileRepoMock.Object;
    public IProfileQueryService ProfileQuery => _profileQueryMock.Object;
    public IEventBus EventBus => _eventBusMock.Object;
}
