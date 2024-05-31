using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Views.ProfileFilters;

namespace Genius.Starlog.UI.Tests.Views;

public sealed class TimeAgoProfileFilterSettingsViewModelTests
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture();

    [Fact]
    public void Ctor_FillsUpData()
    {
        // Arrange
        TimeAgoProfileFilter profileFilter = _fixture.Create<TimeAgoProfileFilter>();

        // Act
        var sut = new TimeAgoProfileFilterSettingsViewModel(profileFilter);

        // Verify
        Assert.Equal((int)profileFilter.TimeAgo.TotalMinutes, sut.MinAgo);
        Assert.Equal(profileFilter.TimeAgo.Seconds, sut.SecAgo);
    }

    [Fact]
    public void CommitChanges()
    {
        // Arrange
        TimeAgoProfileFilter profileFilter = _fixture.Create<TimeAgoProfileFilter>();
        var initialTimeAgo = profileFilter.TimeAgo;
        var sut = new TimeAgoProfileFilterSettingsViewModel(profileFilter);
        sut.MinAgo = _fixture.Create<int>();
        sut.SecAgo = _fixture.Create<int>() % 59;

        // Act
        sut.CommitChanges();

        // Verify
        Assert.NotEqual(initialTimeAgo, profileFilter.TimeAgo);
        Assert.Equal(sut.MinAgo, (int)profileFilter.TimeAgo.TotalMinutes);
        Assert.Equal(sut.SecAgo, profileFilter.TimeAgo.Seconds);
    }

    [Fact]
    public void ResetChanges()
    {
        // Arrange
        TimeAgoProfileFilter profileFilter = _fixture.Create<TimeAgoProfileFilter>();
        var initialTimeAgo = profileFilter.TimeAgo;
        var sut = new TimeAgoProfileFilterSettingsViewModel(profileFilter);
        sut.MinAgo = _fixture.Create<int>();
        sut.SecAgo = _fixture.Create<int>() % 59;

        // Act
        sut.ResetChanges();

        // Verify
        Assert.Equal(initialTimeAgo, profileFilter.TimeAgo);
        Assert.Equal(sut.MinAgo, (int)profileFilter.TimeAgo.TotalMinutes);
        Assert.Equal(sut.SecAgo, profileFilter.TimeAgo.Seconds);
    }
}
