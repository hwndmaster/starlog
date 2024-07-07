using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.Core.Tests.LogFlow;

public sealed class LocContainerTests
{
    private readonly Fixture _fixture = InfrastructureTestHelper.CreateFixture();

    [Fact]
    public void SourceCount_WhenSourceAddedOrRemoved_CountChanged()
    {
        // Arrange
        using var sut = new LogContainer();
        var source1 = new DummyLogSource(_fixture);
        var source2 = new DummyLogSource(_fixture);

        // Pre-verify
        Assert.Equal(0, sut.SourcesCount);

        // Act & Verify + 1
        sut.AddSource(source1);
        Assert.Equal(1, sut.SourcesCount);

        // Act & Verify + 1
        sut.AddSource(source2);
        Assert.Equal(2, sut.SourcesCount);

        // Act & Verify - 1
        sut.RemoveSource(source1.Name);
        Assert.Equal(1, sut.SourcesCount);
    }

    private class DummyLogSource : LogSourceBase
    {
        public DummyLogSource(IFixture fixture)
            => Name = fixture.Create<string>();
        public override LogSourceBase WithNewName(string newName) => throw new NotImplementedException();
        public override string Name { get; }
    }
}
