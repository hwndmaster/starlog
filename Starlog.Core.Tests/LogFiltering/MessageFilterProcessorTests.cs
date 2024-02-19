using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Tests.LogFiltering;

public sealed class MessageFilterProcessorTests
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture(useMutableValueTypeGenerator: true);

    [Theory]
    [InlineData(false, false, false, "test", "message TeSt with content.", "", true)]
    [InlineData(false, false, false, "test", "message Te St with content.", "", false)]
    [InlineData(false, true, false, "Test", "message Test with content.", "", true)]
    [InlineData(false, true, false, "test", "message Test with content.", "", false)]
    [InlineData(true, false, false, "t[es]{2}t", "message TeSt with content.", "", true)]
    [InlineData(true, false, false, "t[ss]{2}t", "message Test with content.", "", false)]
    [InlineData(true, true, false, "t[SsEe]{2}t", "message tESt with content.", "", true)]
    [InlineData(true, true, false, "t[sEe]{2}t", "message tESt with content.", "", false)]
    [InlineData(false, false, true, "test", "message with content.", "artifact TeSt with content.", true)]
    [InlineData(false, false, true, "test", "message with content.", "Te St with content.", false)]
    [InlineData(false, true, true, "Test", "message with content.", "artifact Test with content.", true)]
    [InlineData(false, true, true, "test", "message with content.", "artifact Test with content.", false)]
    [InlineData(true, false, true, "t[es]{2}t", "message with content.", "artifact TeSt with content.", true)]
    [InlineData(true, false, true, "t[ss]{2}t", "message with content.", "artifact Test with content.", false)]
    [InlineData(true, true, true, "t[SsEe]{2}t", "message with content.", "artifact tESt with content.", true)]
    [InlineData(true, true, true, "t[sEe]{2}t", "message with content.", "artifact tESt with content.", false)]
    public void IsMatch_Scenarios(bool regex, bool casing, bool artifacts, string pattern, string message, string artifact, bool expected)
    {
        // Arrange
        var sut = new MessageFilterProcessor();
        var profileFilter = new MessageProfileFilter(_fixture.Create<LogFilter>())
        {
            Pattern = pattern,
            IsRegex = regex,
            MatchCasing = casing,
            IncludeArtifacts = artifacts
        };
        var logRecord = _fixture.Build<LogRecord>()
            .With(x => x.Message, message)
            .With(x => x.LogArtifacts, artifact)
            .Create();

        // Act
        var actual = sut.IsMatch(profileFilter, logRecord);

        // Verify
        Assert.Equal(expected, actual);

        // Arrange over excluded filter
        profileFilter.Exclude = true;

        // Act
        actual = sut.IsMatch(profileFilter, logRecord);

        // Verify
        Assert.Equal(!expected, actual);
    }
}
