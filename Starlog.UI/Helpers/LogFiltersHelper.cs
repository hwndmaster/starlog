using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.ViewModels;

namespace Genius.Starlog.UI.Helpers;

public record LogFilterContext(
    bool MessageSearchIncluded,
    string SearchText,
    Regex? MessageSearchRegex,
    HashSet<string> FilesSelected,
    ImmutableArray<ProfileFilterBase> FiltersSelected);

public static class LogFiltersHelper
{
    internal static void InitializeQuickFiltersCategory(LogFilterCategoryViewModel<LogFilterViewModel> quickFiltersCategory)
    {
        // TODO: throw new NotImplementedException();
    }

    internal static LogFilterContext CreateContext(ICollection<ILogFilterCategoryViewModel> selectedFilters, string searchText, bool searchRegex)
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

    internal static bool IsMatch(LogFilterContext? context, LogItemViewModel item)
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

        // TODO: Add other filters here

        return true;
    }
}
