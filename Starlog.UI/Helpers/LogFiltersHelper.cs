using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.ViewModels;

namespace Genius.Starlog.UI.Helpers;

public record LogFilterContext(
    bool MessageSearchIncluded,
    string SearchText,
    Regex? MessageSearchRegex,
    HashSet<string> FilesSelected,
    ImmutableArray<ProfileFilterBase> FiltersSelected,
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo);

public interface ILogFiltersHelper
{
    void InitializeQuickFiltersCategory(LogFilterCategoryViewModel<LogFilterViewModel> category);
    LogFilterContext CreateContext(ICollection<ILogFilterNodeViewModel> selectedFilters, ILogsSearchViewModel searchViewModel);
    bool IsMatch(LogFilterContext? context, LogItemViewModel item);
}

public sealed class LogFiltersHelper : ILogFiltersHelper
{
    private readonly ILogFilterContainer _logFilterContainer;

    public LogFiltersHelper(ILogFilterContainer logFilterContainer)
    {
        _logFilterContainer = logFilterContainer.NotNull();
    }

    public void InitializeQuickFiltersCategory(LogFilterCategoryViewModel<LogFilterViewModel> category)
    {
        var filter1 = _logFilterContainer.CreateProfileFilter<LogSeveritiesProfileFilter>("Warnings");
        filter1.LogSeverities = new [] { LogSeverity.Warning };

        var filter2 = _logFilterContainer.CreateProfileFilter<LogSeveritiesProfileFilter>("Majors and Criticals");
        filter2.LogSeverities = new [] { LogSeverity.Major, LogSeverity.Critical };

        category.AddItems(new [] {
                filter1,
                filter2
            }
            .Select(x => new LogFilterViewModel(x, isUserDefined: false)));
    }

    public LogFilterContext CreateContext(ICollection<ILogFilterNodeViewModel> selectedFilters, ILogsSearchViewModel searchViewModel)
    {
        var filesSelected = selectedFilters.OfType<LogFileViewModel>()
            .Select(x => x.File.FileName)
            .ToHashSet();
        var filtersSelected = selectedFilters.OfType<LogFilterViewModel>()
            .Select(x => x.Filter)
            .ToImmutableArray();
        var messageSearchIncluded = !string.IsNullOrWhiteSpace(searchViewModel.Text);

        Regex? filterRegex = null;
        if (searchViewModel.UseRegex)
        {
            try
            {
                filterRegex = new Regex(searchViewModel.Text, RegexOptions.IgnoreCase);
            }
            catch (Exception)
            {
                filterRegex = null;
            }
        }

        DateTimeOffset? dateFrom = null;
        DateTimeOffset? dateTo = null;
        if (searchViewModel.MinDateTimeTicks != searchViewModel.SelectedDateTimeFromTicks
            || searchViewModel.MaxDateTimeTicks != searchViewModel.SelectedDateTimeToTicks)
        {
            dateFrom = new DateTimeOffset((long)searchViewModel.SelectedDateTimeFromTicks, TimeSpan.Zero);
            dateTo = new DateTimeOffset((long)searchViewModel.SelectedDateTimeToTicks, TimeSpan.Zero);
        }

        return new(messageSearchIncluded, searchViewModel.Text, filterRegex, filesSelected, filtersSelected, dateFrom, dateTo);
    }

    public bool IsMatch(LogFilterContext? context, LogItemViewModel item)
    {
        if (context is null)
        {
            return true;
        }

        if (context.FilesSelected.Count > 0
            && !context.FilesSelected.Contains(item.Record.File.FileName))
        {
            return false;
        }

        if (context.DateFrom is not null && context.DateTo is not null)
        {
            if (item.DateTime < context.DateFrom || item.DateTime > context.DateTo)
            {
                return false;
            }
        }

        if (context.MessageSearchIncluded)
        {
            bool filterMatch;
            if (context.MessageSearchRegex is not null)
            {
                filterMatch = context.MessageSearchRegex.IsMatch(item.Message);
            }
            else
            {
                filterMatch = item.Message.Contains(context.SearchText, StringComparison.InvariantCultureIgnoreCase);
            }

            if (!filterMatch)
            {
                return false;
            }
        }

        foreach (var filter in context.FiltersSelected)
        {
            var processor = _logFilterContainer.GetFilterProcessor(filter);
            if (!processor.IsMatch(filter, item.Record))
            {
                return false;
            }
        }

        return true;
    }
}
