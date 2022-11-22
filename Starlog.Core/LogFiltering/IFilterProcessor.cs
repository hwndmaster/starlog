using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

public interface IFilterProcessor
{
    IEnumerable<LogRecord> Filter(ProfileFilterBase profileFilter, IEnumerable<LogRecord> logs);
}
