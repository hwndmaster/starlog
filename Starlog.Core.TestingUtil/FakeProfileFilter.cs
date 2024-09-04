using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.TestingUtil;

public sealed class FakeProfileFilter : ProfileFilterBase
{
    public FakeProfileFilter(Guid? id = null)
        : base(new LogFilter(Guid.NewGuid(), Guid.NewGuid().ToString()))
    {
        if (id is not null)
        {
            Id = id.Value;
        }
    }
}
