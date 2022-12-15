using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFiltering;

public interface IFilterProcessor
{
    bool IsMatch(ProfileFilterBase profileFilter, LogRecord log);
}
