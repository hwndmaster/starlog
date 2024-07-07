using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.Tests.Views;

public sealed class PatternValueViewModelTests
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture();

    [Fact]
    public void WhenTypeChangedToRegularExpression_SetIsRegexToTrue()
    {
        // Arrange
        var sut = new PatternValueViewModel(_fixture.Build<PatternValue>()
            .With(x => x.Type, (PatternType)999) // Random, not existing
            .Create());

        // Act
        sut.Type = PatternType.RegularExpression;

        // Verify
        Assert.True(sut.IsRegex);
    }

    [Fact]
    public void WhenTypeChangedToMaskPattern_SetIsRegexToFalse()
    {
        // Arrange
        var sut = new PatternValueViewModel(_fixture.Build<PatternValue>()
            .With(x => x.Type, (PatternType)999) // Random, not existing
            .Create());

        // Act
        sut.Type = PatternType.MaskPattern;

        // Verify
        Assert.False(sut.IsRegex);
    }

    [Fact]
    public void Commit_ReturnsUpdatedValues()
    {
        // Arrange
        var patternValue = _fixture.Build<PatternValue>()
            .With(x => x.Type, PatternType.MaskPattern)
            .Create();
        var sut = new PatternValueViewModel(patternValue);
        sut.Type = PatternType.RegularExpression;
        sut.Name = _fixture.Create<string>();
        sut.Pattern = _fixture.Create<string>();

        // Pre-verify
        Assert.NotEqual(patternValue.Type, sut.Type);
        Assert.NotEqual(patternValue.Name, sut.Name);
        Assert.NotEqual(patternValue.Pattern, sut.Pattern);

        // Act
        var result = sut.Commit();

        // Verify
        Assert.NotEqual(result, patternValue); // Created object is not the same as original
        Assert.Equal(result.Id, patternValue.Id); // Id remained unchanged
        Assert.Equal(result.Type, sut.Type);
        Assert.NotEqual(result.Type, patternValue.Type);
        Assert.Equal(result.Name, sut.Name);
        Assert.NotEqual(result.Name, patternValue.Name);
        Assert.Equal(result.Pattern, sut.Pattern);
        Assert.NotEqual(result.Pattern, patternValue.Pattern);
    }

    [Fact]
    public void Validate_Pattern_NotEmptyOrNull()
    {
        // Arrange
        var sut = new PatternValueViewModel(_fixture.Create<PatternValue>());
        sut.Pattern = null!;

        // Act & Verify # 1
        sut.Validate();
        Assert.True(sut.HasErrors);

        // Re-arrange
        sut.Pattern = string.Empty;

        // Act & Verify # 1
        sut.Validate();
        Assert.True(sut.HasErrors);

        // Re-arrange
        sut.Pattern = @"\w";

        // Act & Verify # 1
        sut.Validate();
        Assert.False(sut.HasErrors);
    }

    [Fact]
    public void Validate_Pattern_IsCorrectRegex()
    {
        // Arrange
        var sut = new PatternValueViewModel(_fixture.Create<PatternValue>());
        sut.Pattern = @"\";
        sut.Type = PatternType.MaskPattern;

        // Act & Verify # 1
        sut.Validate();
        Assert.False(sut.HasErrors);

        // Re-arrange
        sut.Type = PatternType.RegularExpression;

        // Act & Verify # 1
        sut.Validate();
        Assert.True(sut.HasErrors);

        // Re-arrange
        sut.Pattern = @"\w";

        // Act & Verify # 1
        sut.Validate();
        Assert.False(sut.HasErrors);
    }
}
