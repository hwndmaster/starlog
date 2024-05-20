using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.CommandHandlers;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.Tests.CommandHandlers;

public sealed class SettingsUpdateCommandHandlerTests
{
    private readonly ISettingsRepository _repoMock = A.Fake<ISettingsRepository>();
    private readonly IEventBus _eventBusMock = A.Fake<IEventBus>();
    private readonly SettingsUpdateCommandHandler _sut;

    public SettingsUpdateCommandHandlerTests()
    {
        _sut = new(_repoMock, _eventBusMock);
    }

    [Fact]
    public async Task Process_HappyFlowScenario()
    {
        // Arrange
        var settings = new Settings();
        var command = new SettingsUpdateCommand(settings);

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        A.CallTo(() => _repoMock.Store(settings)).MustHaveHappened();
        A.CallTo(() => _eventBusMock.Publish(A<SettingsUpdatedEvent>.That.Matches(x => x.Settings == settings))).MustHaveHappened();
    }
}
