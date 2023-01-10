namespace Genius.Starlog.UI.Tests.ValueConverters;

public sealed class TestForFailingCiPipeline
{
    [Fact]
    public void FailingTest()
    {
        Assert.Fail("Got it!");
    }
}
