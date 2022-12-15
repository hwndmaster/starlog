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
    }
}
