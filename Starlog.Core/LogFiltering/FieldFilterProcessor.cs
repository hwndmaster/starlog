using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFiltering;

public sealed class FieldFilterProcessor : IFilterProcessor
{
    private readonly ILogContainer _logContainer;

    public FieldFilterProcessor(ILogContainer logContainer)
    {
        _logContainer = logContainer.NotNull();
    }

    public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log)
    {
        var filter = (FieldProfileFilter)profileFilter;

        var fieldValueId = log.FieldValueIndices[filter.FieldId];
        var fieldValue = _logContainer.GetFields().GetFieldValue(filter.FieldId, fieldValueId);
        var result = filter.Values.Contains(fieldValue, StringComparer.OrdinalIgnoreCase);

        return filter.Exclude ? !result : result;
    }
}
