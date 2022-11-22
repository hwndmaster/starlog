using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

public sealed class ThreadFilterProcessor : IFilterProcessor
{
    public IEnumerable<LogRecord> Filter(ProfileFilterBase profileFilter, IEnumerable<LogRecord> logs)
    {
        var filter = (ThreadProfileFilter)profileFilter;

        // TODO: Implement filter processing logic here

        throw new NotImplementedException();
    }
}
