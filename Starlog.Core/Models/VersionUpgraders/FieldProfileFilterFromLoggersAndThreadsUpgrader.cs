using Genius.Atom.Data.Persistence;
using Genius.Starlog.Core.LogFiltering;

namespace Genius.Starlog.Core.Models.VersionUpgraders;

/// <summary>
///   This upgrade is applicable only to profiles created for Starfix.
///   Thus, "thread" field is expected to be '0', and 'loggers' to be '1'.
///   So far no other profiles were expected before this upgrade.
/// </summary>
public class FieldProfileFilterFromLoggersAndThreadsUpgrader
    : IDataVersionUpgrader<LoggersProfileFilter, FieldProfileFilter>
    , IDataVersionUpgrader<ThreadsProfileFilter, FieldProfileFilter>
{
    private readonly ILogFilterContainer _filterContainer;

    public FieldProfileFilterFromLoggersAndThreadsUpgrader(ILogFilterContainer filterContainer)
    {
        _filterContainer = filterContainer.NotNull();
    }

    public FieldProfileFilter Upgrade(LoggersProfileFilter value)
    {
        var profileFilter = _filterContainer.CreateProfileFilter<FieldProfileFilter>(value.Name);
        profileFilter.FieldId = 1;
        profileFilter.Exclude = value.Exclude;
        profileFilter.Values = value.LoggerNames;
        return profileFilter;
    }

    public FieldProfileFilter Upgrade(ThreadsProfileFilter value)
    {
        var profileFilter = _filterContainer.CreateProfileFilter<FieldProfileFilter>(value.Name);
        profileFilter.FieldId = 0;
        profileFilter.Exclude = value.Exclude;
        profileFilter.Values = value.Threads;
        return profileFilter;
    }
}
