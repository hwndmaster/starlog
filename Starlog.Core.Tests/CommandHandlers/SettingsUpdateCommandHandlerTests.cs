using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.CommandHandlers;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.Tests.CommandHandlers;

public sealed class SettingsUpdateCommandHandlerTests
{
    private readonly Mock<ISettingsRepository> _repoMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly SettingsUpdateCommandHandler _sut;

    public SettingsUpdateCommandHandlerTests()
    {
        _sut = new(_repoMock.Object, _eventBusMock.Object);
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
        _repoMock.Verify(x => x.Store(settings));
        _eventBusMock.Verify(x => x.Publish(It.Is<SettingsUpdatedEvent>(x => x.Settings == settings)));
    }
}
