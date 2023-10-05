using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

public interface IComparisonService
{
    Task<ComparisonContext?> LoadProfilesAsync(Profile profile1, Profile profile2);
}

internal sealed class ComparisonService : IComparisonService
{
    private readonly IProfileLoader _profileLoader;

    public ComparisonService(IProfileLoader profileLoader)
    {
        _profileLoader = profileLoader.NotNull();
    }

    // TODO: Cover with unit tests
    public async Task<ComparisonContext?> LoadProfilesAsync(Profile profile1, Profile profile2)
    {
        var logContainer1 = new LogContainer();
        var logContainer2 = new LogContainer();

        var taskProfileLoading1 = _profileLoader.LoadProfileAsync(profile1, logContainer1);
        var taskProfileLoading2 = _profileLoader.LoadProfileAsync(profile2, logContainer2);

        await Task.WhenAll(taskProfileLoading1, taskProfileLoading2).ConfigureAwait(false);

        if (!taskProfileLoading1.Result || !taskProfileLoading2.Result)
        {
            return null;
        }

        // TODO: Implement comparison

        return new ComparisonContext(profile1, logContainer1, profile2, logContainer2);
    }
}
