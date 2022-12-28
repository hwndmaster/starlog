using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFiltering;

public interface IQuickFilterProvider
{
    IEnumerable<ProfileFilterBase> GetQuickFilters();
}

public sealed class QuickFilterProvider : IQuickFilterProvider
{
    private readonly ILogFilterContainer _logFilterContainer;

    public QuickFilterProvider(ILogFilterContainer logFilterContainer)
    {
        _logFilterContainer = logFilterContainer.NotNull();
    }

    public IEnumerable<ProfileFilterBase> GetQuickFilters()
    {
        var filter1 = _logFilterContainer.CreateProfileFilter<LogSeveritiesProfileFilter>("Warnings");
        filter1.LogSeverities = new [] { LogSeverity.Warning };
        yield return filter1;

        var filter2 = _logFilterContainer.CreateProfileFilter<LogSeveritiesProfileFilter>("Majors and Criticals");
        filter2.LogSeverities = new [] { LogSeverity.Major, LogSeverity.Critical };
        yield return filter2;

        var filter3 = _logFilterContainer.CreateProfileFilter<MessageProfileFilter>("Contains 'Exception'");
        filter3.Pattern = "Exception";
        filter3.IncludeArtifacts = true;
        filter3.MatchCasing = true;
        filter3.IsRegex = false;
        yield return filter3;

        var filter4 = _logFilterContainer.CreateProfileFilter<ThreadsProfileFilter>("Main Thread");
        filter4.Threads = new [] { "Main", "1" };
        yield return filter4;

        var filter5 = _logFilterContainer.CreateProfileFilter<ThreadsProfileFilter>("Other Threads");
        filter5.Exclude = true;
        filter5.Threads = new [] { "Main", "1" };
        yield return filter5;
    }
}
