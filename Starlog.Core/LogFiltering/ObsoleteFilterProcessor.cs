using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFiltering;

[Obsolete("Used for backwards compatibility only. To be removed in the next major version.")]
internal sealed class ObsoleteFilterProcessor : IFilterProcessor
{
    public bool IsMatch(ProfileFilterBase profileFilter, LogRecord log)
    {
        throw new NotSupportedException("This filter processor is obsolete and cannot be used anymore.");
    }
}
