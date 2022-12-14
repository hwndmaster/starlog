using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.ViewModels;

namespace Genius.Starlog.UI.Helpers;

public record LogOverallFilterContext(LogFilterContext Filter, LogSearchContext Search);

public record LogFilterContext(
    HashSet<string> FilesSelected,
    ImmutableArray<ProfileFilterBase> FiltersSelected);

public record LogSearchContext(
    bool MessageSearchIncluded,
    string SearchText,
    Regex? MessageSearchRegex,
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo);

public interface ILogFiltersHelper
{
    void InitializeQuickFiltersCategory(LogFilterCategoryViewModel<LogFilterViewModel> category);
    void InitializePinSubscription(IEnumerable<LogFilterViewModel> items, Action handler);
    bool IsMatch(LogOverallFilterContext? context, ILogItemViewModel item);
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

    public void InitializePinSubscription(IEnumerable<LogFilterViewModel> items, Action handler)
    {
        foreach (var item in items)
        {
            if (item.CanPin)
            {
                item.WhenChanged(x => x.IsPinned).Subscribe(_ => handler());
            }
        }
    }

    public bool IsMatch(LogOverallFilterContext? context, ILogItemViewModel item)
    {
        if (context is null)
        {
            return true;
        }

        if (context.Filter.FilesSelected.Count > 0
            && !context.Filter.FilesSelected.Contains(item.Record.File.FileName))
        {
            return false;
        }

        if (context.Search.DateFrom is not null && context.Search.DateTo is not null)
        {
            if (item.Record.DateTime < context.Search.DateFrom || item.Record.DateTime > context.Search.DateTo)
            {
                return false;
            }
        }

        foreach (var filter in context.Filter.FiltersSelected)
        {
            var processor = _logFilterContainer.GetFilterProcessor(filter);
            if (!processor.IsMatch(filter, item.Record))
            {
                return false;
            }
        }

        if (context.Search.MessageSearchIncluded)
        {
            bool filterMatch;
            if (context.Search.MessageSearchRegex is not null)
            {
                filterMatch = context.Search.MessageSearchRegex.IsMatch(item.Message);
            }
            else
            {
                filterMatch = item.Message.Contains(context.Search.SearchText, StringComparison.InvariantCultureIgnoreCase);
            }

            if (!filterMatch)
            {
                return false;
            }
        }

        return true;
    }
}
