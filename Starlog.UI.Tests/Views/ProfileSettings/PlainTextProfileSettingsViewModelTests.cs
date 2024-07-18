using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Views.ProfileSettings;

namespace Genius.Starlog.UI.Tests.Views;

public sealed class PlainTextProfileSettingsViewModelTests
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly ISettingsQueryService _settingsQueryFake = A.Fake<ISettingsQueryService>();
    private readonly PatternValue[] _patternValues;

    public PlainTextProfileSettingsViewModelTests()
    {
        _patternValues = _fixture.CreateMany<PatternValue>().ToArray();
        var settings = new Settings
        {
            PlainTextLogCodecLinePatterns = _patternValues,
        };
        A.CallTo(() => _settingsQueryFake.Get()).Returns(settings);
    }

    [Fact]
    public void Ctor_FillsUpData()
    {
        // Arrange
        var profileSettings = SetupSettings();

        // Act
        var sut = CreateSystemUnderTest(profileSettings);

        // Verify
        Assert.Equal(sut.DateTimeFormat, profileSettings.DateTimeFormat);
        Assert.Equal(sut.FileArtifactLinesCount, profileSettings.FileArtifactLinesCount);
        Assert.Equal(sut.LinePattern.Id, profileSettings.LinePatternId);
        Assert.Equal(sut.LogsLookupPattern, profileSettings.LogsLookupPattern);
        Assert.Equal(sut.Path, profileSettings.Paths[0]);
        Assert.Equal(
            _patternValues.Select(x => (x.Name, x.Pattern, x.Type)),
            sut.LinePatterns.Select(x => (x.Name, x.Pattern, x.Type))
        );
    }

    [Fact]
    public void CopySettingsFrom_GivenAppropriateTypeOfVmInstance()
    {
        // Arrange
        var profileSettings = SetupSettings();
        var sut = CreateSystemUnderTest(profileSettings);
        var copyFrom = CreateSystemUnderTest(SetupSettings());
        copyFrom.LinePattern = copyFrom.LinePatterns[2]; // [1] is preset in `SetupSettings()`

        // Pre-verify
        Assert.NotEqual(sut.DateTimeFormat, copyFrom.DateTimeFormat);
        Assert.NotEqual(sut.FileArtifactLinesCount, copyFrom.FileArtifactLinesCount);
        Assert.NotEqual(sut.LinePattern.Id, copyFrom.LinePattern.Id);
        Assert.NotEqual(sut.LogsLookupPattern, copyFrom.LogsLookupPattern);
        Assert.NotEqual(sut.Path, copyFrom.Path);

        // Act
        sut.CopySettingsFrom(copyFrom);

        // Verify
        Assert.Equal(sut.DateTimeFormat, copyFrom.DateTimeFormat);
        Assert.Equal(sut.FileArtifactLinesCount, copyFrom.FileArtifactLinesCount);
        Assert.Equal(sut.LinePattern.Id, copyFrom.LinePattern.Id);
        Assert.Equal(sut.LogsLookupPattern, copyFrom.LogsLookupPattern);
        Assert.Equal(sut.Path, copyFrom.Path);
    }

    [Fact]
    public void CommitChanges_GivenValidationOk_ThenUpdateValuesInModel()
    {
        // Arrange
        var profileSettings = SetupSettings();
        var sut = CreateSystemUnderTest(profileSettings);
        sut.DateTimeFormat = _fixture.Create<string>();
        sut.FileArtifactLinesCount = _fixture.Create<int>();
        sut.LinePattern = sut.LinePatterns[2]; // [1] is preset in `SetupSettings()`
        sut.LogsLookupPattern = _fixture.Create<string>();
        sut.Path = @"C:\"; // Need to satisfy `PathExistsValidationRule`

        // Pre-verify
        Assert.NotEqual(sut.DateTimeFormat, profileSettings.DateTimeFormat);
        Assert.NotEqual(sut.FileArtifactLinesCount, profileSettings.FileArtifactLinesCount);
        Assert.NotEqual(sut.LinePattern.Id, profileSettings.LinePatternId);
        Assert.NotEqual(sut.LogsLookupPattern, profileSettings.LogsLookupPattern);
        Assert.NotEqual(sut.Path, profileSettings.Paths[0]);

        // Act
        sut.CommitChanges();

        // Verify
        Assert.Equal(sut.DateTimeFormat, profileSettings.DateTimeFormat);
        Assert.Equal(sut.FileArtifactLinesCount, profileSettings.FileArtifactLinesCount);
        Assert.Equal(sut.LinePattern.Id, profileSettings.LinePatternId);
        Assert.Equal(sut.LogsLookupPattern, profileSettings.LogsLookupPattern);
        Assert.Equal(sut.Path, profileSettings.Paths[0]);
    }

    [Fact]
    public void CommitChanges_GivenValidationFailed_ThenUpdateValuesInModel()
    {
        // Arrange
        var profileSettings = SetupSettings();
        var sut = CreateSystemUnderTest(profileSettings);
        sut.DateTimeFormat = _fixture.Create<string>();
        sut.FileArtifactLinesCount = _fixture.Create<int>();
        sut.LinePattern = sut.LinePatterns[2]; // [1] is preset in `SetupSettings()`
        sut.LogsLookupPattern = _fixture.Create<string>();
        sut.Path = _fixture.Create<string>(); // Need to fail the `PathExistsValidationRule` rule.

        // Act
        sut.CommitChanges();

        // Verify
        Assert.NotEqual(sut.DateTimeFormat, profileSettings.DateTimeFormat);
        Assert.NotEqual(sut.FileArtifactLinesCount, profileSettings.FileArtifactLinesCount);
        Assert.NotEqual(sut.LinePattern.Id, profileSettings.LinePatternId);
        Assert.NotEqual(sut.LogsLookupPattern, profileSettings.LogsLookupPattern);
        Assert.NotEqual(sut.Path, profileSettings.Paths[0]);
    }

    private PlainTextProfileSettings SetupSettings()
    {
        return new PlainTextProfileSettings(_fixture.Create<LogCodec>())
        {
            DateTimeFormat = _fixture.Create<string>(),
            FileArtifactLinesCount = _fixture.Create<int>(),
            LinePatternId = _patternValues[1].Id,
            LogsLookupPattern = _fixture.Create<string>(),
            Paths = [_fixture.Create<string>()]
        };
    }

    private PlainTextProfileSettingsViewModel CreateSystemUnderTest(PlainTextProfileSettings profileSettings)
    {
        return new PlainTextProfileSettingsViewModel(profileSettings, _settingsQueryFake);
    }
}
