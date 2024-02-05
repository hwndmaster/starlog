using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.TestingUtil;

public sealed class TestProfileSettings : ProfileSettingsBase
{
    public TestProfileSettings() : base(new Fixture().Create<LogCodec>())
    {
    }

    public override string Source => throw new NotImplementedException();

    internal override ProfileSettingsBase CloneInternal()
    {
        throw new NotImplementedException();
    }
}
