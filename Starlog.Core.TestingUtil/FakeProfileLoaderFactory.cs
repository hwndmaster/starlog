using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.ProfileLoading;

namespace Genius.Starlog.Core.TestingUtil;

internal sealed class FakeProfileLoaderFactory : IProfileLoaderFactory
{
    public IProfileLoader? InstanceToReturn { get; set; }

    public IProfileLoader? Create(Profile profile)
    {
        return InstanceToReturn ?? A.Fake<IProfileLoader>();
    }
}
