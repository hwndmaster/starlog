using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFiltering;

public interface ILogFilterContainer
{
    ProfileFilterBase CreateProfileFilter(LogFilter logFilter);
    TLogProfileFilter CreateProfileFilter<TLogProfileFilter>(string? name = null)
        where TLogProfileFilter : ProfileFilterBase;

    /// <summary>
    ///   Creates a profile filter with a predefined identifier. Used for "Quick Filters".
    /// </summary>
    /// <typeparam name="TLogProfileFilter">The type of the filter.</typeparam>
    /// <param name="name">The name of the filter.</param>
    /// <param name="predefinedId">The predefined identifier.</param>
    TLogProfileFilter CreateProfileFilter<TLogProfileFilter>(string name, Guid predefinedId)
        where TLogProfileFilter : ProfileFilterBase;

    LogFilter GetLogFilter(Guid logFilterId);
    IEnumerable<LogFilter> GetLogFilters();
    IFilterProcessor GetFilterProcessor(ProfileFilterBase profileFilter);
    void RegisterLogFilter<TProfileFilter, TFilterProcessor>(LogFilter logFilter)
        where TProfileFilter : ProfileFilterBase
        where TFilterProcessor : class, IFilterProcessor;
}

internal sealed class LogFilterContainer : ILogFilterContainer
{
    private readonly Dictionary<Guid /* LogFilter.Id */, LogFilterRecord> _registeredFilters = new();

    private readonly IFilterProcessor[] _filterProcessors;

    public LogFilterContainer(IEnumerable<IFilterProcessor> filterProcessors)
    {
        _filterProcessors = filterProcessors.ToArray();
    }

    public TLogProfileFilter CreateProfileFilter<TLogProfileFilter>(string? name = null)
        where TLogProfileFilter : ProfileFilterBase
    {
        return CreateProfileFilterInternal<TLogProfileFilter>(name, null);
    }

    public TLogProfileFilter CreateProfileFilter<TLogProfileFilter>(string name, Guid predefinedId)
        where TLogProfileFilter : ProfileFilterBase
    {
        return CreateProfileFilterInternal<TLogProfileFilter>(name, predefinedId);
    }

    public ProfileFilterBase CreateProfileFilter(LogFilter logFilter)
    {
        return CreateProfileFilterInternal(logFilter, null);
    }

    public LogFilter GetLogFilter(Guid logFilterId)
    {
        return _registeredFilters[logFilterId].LogFilter;
    }

    public IEnumerable<LogFilter> GetLogFilters()
    {
        return _registeredFilters.Select(x => x.Value.LogFilter);
    }

    public IFilterProcessor GetFilterProcessor(ProfileFilterBase profileFilter)
    {
        if (!_registeredFilters.TryGetValue(profileFilter.LogFilter.Id, out var value))
        {
            throw new InvalidOperationException("The log filter '" + profileFilter.LogFilter.Id + "' doesn't exists.");
        }

        return _filterProcessors.First(x => x.GetType() == value.FilterProcessorType);
    }

    public void RegisterLogFilter<TProfileFilter, TFilterProcessor>(LogFilter logFilter)
        where TProfileFilter : ProfileFilterBase
        where TFilterProcessor : class, IFilterProcessor
    {
        if (_registeredFilters.ContainsKey(logFilter.Id))
        {
            throw new InvalidOperationException("The log filter '" + logFilter.Id + "' already exists.");
        }

        _registeredFilters.Add(logFilter.Id, new LogFilterRecord(logFilter, typeof(TProfileFilter), typeof(TFilterProcessor)));
    }

    private TLogProfileFilter CreateProfileFilterInternal<TLogProfileFilter>(string? name, Guid? predefinedId)
        where TLogProfileFilter : ProfileFilterBase
    {
        var type = typeof(TLogProfileFilter);
        var registration = _registeredFilters.Values.Single(x => x.ProfileFilterType == type);

        var filter = (TLogProfileFilter)CreateProfileFilterInternal(registration.LogFilter, predefinedId);
        if (name is not null)
        {
            filter.Name = name;
        }
        return filter;
    }

    private ProfileFilterBase CreateProfileFilterInternal(LogFilter logFilter, Guid? predefinedId)
    {
        if (!_registeredFilters.TryGetValue(logFilter.Id, out var value))
        {
            throw new InvalidOperationException("The log filter '" + logFilter.Id + "' doesn't exists.");
        }

        object[] parameters = predefinedId switch
        {
            null => new object[] { logFilter },
            _ => new object[] { logFilter, predefinedId }
        };

        return (ProfileFilterBase)Activator.CreateInstance(value.ProfileFilterType, parameters).NotNull();
    }

    private readonly record struct LogFilterRecord(LogFilter LogFilter, Type ProfileFilterType, Type FilterProcessorType);
}
