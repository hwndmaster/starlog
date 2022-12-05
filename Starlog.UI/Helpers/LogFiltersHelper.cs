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
    ImmutableArray<ProfileFilterBase> FiltersSelected);

public interface ILogFiltersHelper
{
    void InitializeQuickFiltersCategory(LogFilterCategoryViewModel<LogFilterViewModel> category);
    LogFilterContext CreateContext(ICollection<ILogFilterCategoryViewModel> selectedFilters, string searchText, bool searchRegex);
    bool IsMatch(LogFilterContext? context, LogItemViewModel item);
}

public class LogFiltersHelper : ILogFiltersHelper
{
    private readonly ILogFilterContainer _logFilterContainer;

    public LogFiltersHelper(ILogFilterContainer logFilterContainer)
    {
        _logFilterContainer = logFilterContainer.NotNull();
    }

    public void InitializeQuickFiltersCategory(LogFilterCategoryViewModel<LogFilterViewModel> category)
    {
        var errorsFilter = _logFilterContainer.CreateProfileFilter<LogLevelsProfileFilter>("Majors and Criticals");
        errorsFilter.LogLevels = new [] { LogSeverity.Major, LogSeverity.Critical };

        category.AddItems(new [] {
                errorsFilter
            }
            .Select(x => new LogFilterViewModel(x)));
    }

    public LogFilterContext CreateContext(ICollection<ILogFilterCategoryViewModel> selectedFilters, string searchText, bool searchRegex)
    {
        var filesSelected = selectedFilters.OfType<LogFileViewModel>()
            .Select(x => x.File.FileName)
            .ToHashSet();
        var filtersSelected = selectedFilters.OfType<LogFilterViewModel>()
            .Select(x => x.Filter)
            .ToImmutableArray();
        var messageSearchIncluded = !string.IsNullOrWhiteSpace(searchText);

        Regex? filterRegex = null;
        if (searchRegex)
        {
            try
            {
                filterRegex = new Regex(searchText, RegexOptions.IgnoreCase);
            }
            catch (Exception)
            {
                filterRegex = null;
            }
        }

        return new(messageSearchIncluded, searchText, filterRegex, filesSelected, filtersSelected);
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