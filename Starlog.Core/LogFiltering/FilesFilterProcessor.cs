using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFiltering;

public sealed class FilesFilterProcessor : IFilterProcessor
{
    public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log)
    {
        var filter = (FilesProfileFilter)profileFilter;

        var result = filter.FileNames.Contains(log.Source.DisplayName, StringComparer.OrdinalIgnoreCase);

        return filter.Exclude ? !result : result;
    }
}
