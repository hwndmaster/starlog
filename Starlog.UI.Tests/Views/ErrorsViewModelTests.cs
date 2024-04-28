using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Atom.Infrastructure.TestingUtil.Events;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.TestingUtil;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.Tests.Views;

public sealed class ErrorsViewModelTests
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly ProfileHarness _profileHarness = new();
    private readonly TestEventBus _eventBus = new();
    private readonly ErrorsViewModel _sut;

    public ErrorsViewModelTests()
    {
        _sut = new ErrorsViewModel(_profileHarness.CurrentProfile, _eventBus);
    }

    [Fact]
    public void ClearCommand_ErrorsCleared_AndFlyoutClosed()
    {
        // Arrange
        _sut.Errors = _fixture.Create<string>();
        _sut.IsErrorsFlyoutVisible = true;

        // Act
        _sut.ClearCommand.Execute(null);

        // Verify
        Assert.Empty(_sut.Errors);
        Assert.False(_sut.IsErrorsFlyoutVisible);
    }

    [Fact]
    public void CurrentProfile_WhenProfileClosed_CallsClearCommand()
    {
        // Arrange
        bool clearHandled = false;
        using var _ = _sut.ClearCommand.Executed.Subscribe(_ => clearHandled = true);
        _profileHarness.CreateProfile(setAsCurrent: true);

        // Act
        _profileHarness.CurrentProfile.CloseProfile();

        // Verify
        Assert.True(clearHandled);
    }

    [Fact]
    public void ProfileLoadingErrorEvent_WhenFired_FlyoutOpened_AndErrorsAdded()
    {
        // Arrange
        var initialErrors = _fixture.Create<string>();
        var addedErrors = _fixture.Create<string>();
        _sut.Errors = initialErrors;
        _sut.IsErrorsFlyoutVisible = false;

        // Act
        _eventBus.Publish(new ProfileLoadingErrorEvent(_profileHarness.CreateProfile(), addedErrors));

        // Verify
        Assert.Equal(initialErrors + Environment.NewLine + addedErrors, _sut.Errors);
    }
}
