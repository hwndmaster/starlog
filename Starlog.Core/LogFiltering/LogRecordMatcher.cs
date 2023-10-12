using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.Core.LogFiltering;

public interface ILogRecordMatcher
{
    bool IsMatch(LogRecordMatcherContext? context, LogRecord item);
}

internal sealed class LogRecordMatcher : ILogRecordMatcher
{
    private readonly ILogFilterContainer _logFilterContainer;

    public LogRecordMatcher(ILogFilterContainer logFilterContainer)
    {
        _logFilterContainer = logFilterContainer.NotNull();
    }

    public bool IsMatch(LogRecordMatcherContext? context, LogRecord item)
    {
        if (context is null)
        {
            return true;
        }

        if (context.Filter.FilesSelected.Count > 0
            && !context.Filter.FilesSelected.Contains(item.File.FileName))
        {
            return false;
        }

        if (context.Search.DateFrom is not null && context.Search.DateTo is not null)
        {
            if (item.DateTime < context.Search.DateFrom || item.DateTime > context.Search.DateTo)
            {
                return false;
            }
        }

        bool anyFilterMatched = false;
        foreach (var filter in context.Filter.FiltersSelected)
        {
            var processor = _logFilterContainer.GetFilterProcessor(filter);
            if (!processor.IsMatch(filter, item))
            {
                if (context.Filter.UseOrCombination)
                {
                    continue;
                }
                return false;
            }

            anyFilterMatched = true;
            if (context.Filter.UseOrCombination)
            {
                break;
            }
        }

        if (!anyFilterMatched && context.Filter.FiltersSelected.Length > 0)
        {
            return false;
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
