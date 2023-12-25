using Genius.Starlog.Core.Models;

namespace Starlog.Core.TestingUtil;

public sealed class TestProfileLogCodec : ProfileLogCodecBase
{
    public TestProfileLogCodec() : base(new Fixture().Create<LogCodec>())
    {
    }
}
