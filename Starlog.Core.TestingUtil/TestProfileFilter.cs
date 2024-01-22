using Genius.Starlog.Core.Models;

namespace Starlog.Core.TestingUtil;

public sealed class TestProfileFilter : ProfileFilterBase
{
    public TestProfileFilter(Guid? id = null)
        : base(new LogFilter(Guid.NewGuid(), Guid.NewGuid().ToString()))
    {
        if (id is not null)
        {
            Id = id.Value;
        }
    }
}
