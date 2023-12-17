using Genius.Starlog.Core.Models;

namespace Starlog.UI.Tests.TestUtil
{
    public class TestProfileLogCodec : ProfileLogCodecBase
    {
        public TestProfileLogCodec() : base(new Fixture().Create<LogCodec>())
        {
        }
    }
}
