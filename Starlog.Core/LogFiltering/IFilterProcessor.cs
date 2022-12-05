using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

public interface IFilterProcessor
{
    bool IsMatch(ProfileFilterBase profileFilter, LogRecord log);
}
