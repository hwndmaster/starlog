using Genius.Atom.UI.Forms;

namespace Genius.Starlog.UI.Tests.ValueConverters;

public sealed class ObservableExtensionsTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public void OnOneTimeExecutedBooleanAction_WhenSimpleActionCommand()
    {
        // Arrange
        var handled = 0;
        var actionCommand = new ActionCommand();
        actionCommand.OnOneTimeExecutedBooleanAction()
            .Subscribe(_ => handled++);

        // Act
        actionCommand.Execute(null);

        // Verify
        Assert.Equal(1, handled);

        // Act: more executions
        actionCommand.Execute(null);
        actionCommand.Execute(null);

        // Verify: should not cause more handlings
        Assert.Equal(1, handled);
    }

    [Fact]
    public void OnOneTimeExecutedBooleanAction_WhenAsyncActionCommand()
    {
        // Arrange
        var handled = 0;
        bool booleanValue = false;
        var actionCommand = new ActionCommand(_ => Task.FromResult(booleanValue));
        actionCommand.OnOneTimeExecutedBooleanAction()
            .Subscribe(_ => handled++);

        // Acts and Verifies: when action returns false
        actionCommand.Execute(null);
        Assert.Equal(0, handled);

        // Re-arrange
        booleanValue = true;

        // Acts and Verifies: when action returns true
        actionCommand.Execute(null);
        Assert.Equal(1, handled);
    }
}
