using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.TestingUtil;

internal sealed class TestProfileLoaderFactory : IProfileLoaderFactory
{
    public IProfileLoader? InstanceToReturn { get; set; }

    public IProfileLoader? Create(Profile profile)
    {
        return InstanceToReturn ?? Mock.Of<IProfileLoader>();
    }
}
