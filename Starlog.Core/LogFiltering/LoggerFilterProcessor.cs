using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

public sealed class LoggerFilterProcessor : IFilterProcessor
{
    public IEnumerable<LogRecord> Filter(ProfileFilterBase profileFilter, IEnumerable<LogRecord> logs)
    {
        var filter = (LoggerProfileFilter)profileFilter;

        // TODO: Implement filter processing logic here

        throw new NotImplementedException();
    }
}
