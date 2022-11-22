using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.Core.LogFiltering;

public interface ILogFilterContainer
{
    ProfileFilterBase CreateProfileFilter(LogFilter logFilter);
    IFilterProcessor CreateFilterProcessor(ProfileFilterBase profileFilter);
    void RegisterLogFilter<TProfileFilter, TFilterProcessor>(LogFilter logFilter)
        where TProfileFilter : ProfileFilterBase
        where TFilterProcessor : class, IFilterProcessor;
}

internal readonly record struct LogFilterRecord(LogFilter LogFilter, Type ProfileFilterType, Type FilterProcessorType);

internal sealed class LogFilterContainer : ILogFilterContainer
{
    private readonly Dictionary<Guid /* LogFilter.Id */, LogFilterRecord> _registeredFilters = new();

    private readonly IFilterProcessor[] _filterProcessors;

    public LogFilterContainer(IEnumerable<IFilterProcessor> filterProcessors)
    {
        _filterProcessors = filterProcessors.ToArray();
    }

    public ProfileFilterBase CreateProfileFilter(LogFilter logFilter)
    {
        if (!_registeredFilters.TryGetValue(logFilter.Id, out var value))
        {
            throw new InvalidOperationException("The log filter '" + logFilter.Id + "' doesn't exists.");
        }

        return (ProfileFilterBase)Activator.CreateInstance(value.ProfileFilterType, logFilter).NotNull();
    }

    public IFilterProcessor CreateFilterProcessor(ProfileFilterBase profileFilter)
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
}
