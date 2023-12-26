using Genius.Starlog.Core.Configuration;
using Genius.Starlog.Core.Models;
using Microsoft.Extensions.Options;

namespace Genius.Starlog.Core.LogFiltering;

public interface IQuickFilterProvider
{
    IEnumerable<ProfileFilterBase> GetQuickFilters();
}

public sealed class QuickFilterProvider : IQuickFilterProvider
{
    private readonly ILogFilterContainer _logFilterContainer;
    private readonly LogLevelMappingConfiguration _logLevelMappingConfig;

    public QuickFilterProvider(ILogFilterContainer logFilterContainer, IOptions<LogLevelMappingConfiguration> logLevelMappingConfig)
    {
        _logFilterContainer = logFilterContainer.NotNull();
        _logLevelMappingConfig = logLevelMappingConfig.NotNull().Value;
    }

    public IEnumerable<ProfileFilterBase> GetQuickFilters()
    {
        var filter1 = _logFilterContainer.CreateProfileFilter<LogLevelsProfileFilter>("Warnings",
            new Guid(0, 0, 0, new byte[] { 0, 0, 0, 0, 0, 0, 1, 0 }));
        filter1.LogLevels = _logLevelMappingConfig.TreatAsWarning;
        yield return filter1;

        var filter2 = _logFilterContainer.CreateProfileFilter<LogLevelsProfileFilter>("Errors",
            new Guid(0, 0, 0, new byte[] { 0, 0, 0, 0, 0, 0, 2, 0 }));
        filter2.LogLevels = _logLevelMappingConfig.TreatAsError.Concat(_logLevelMappingConfig.TreatAsCritical).ToArray();
        yield return filter2;

        var filter3 = _logFilterContainer.CreateProfileFilter<MessageProfileFilter>("Contains 'Exception'",
            new Guid(0, 0, 0, new byte[] { 0, 0, 0, 0, 0, 0, 3, 0 }));
        filter3.Pattern = "Exception";
        filter3.IncludeArtifacts = true;
        filter3.MatchCasing = true;
        filter3.IsRegex = false;
        yield return filter3;

        var filter4 = _logFilterContainer.CreateProfileFilter<ThreadsProfileFilter>("Main Thread",
            new Guid(0, 0, 0, new byte[] { 0, 0, 0, 0, 0, 0, 4, 0 }));
        filter4.Threads = new [] { "Main", "1", string.Empty };
        yield return filter4;

        var filter5 = _logFilterContainer.CreateProfileFilter<ThreadsProfileFilter>("Other Threads",
            new Guid(0, 0, 0, new byte[] { 0, 0, 0, 0, 0, 0, 5, 0 }));
        filter5.Exclude = true;
        filter5.Threads = new [] { "Main", "1", string.Empty };
        yield return filter5;
    }
}
