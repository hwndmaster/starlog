using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.TestingUtil;

public sealed class TestProfileSettings : ProfileSettingsBase
{
    public TestProfileSettings()
        : base(new LogCodec(Guid.NewGuid(), Guid.NewGuid().ToString()))
    {
    }

    public TestProfileSettings(LogCodec logCodec)
        : base(logCodec)
    {
    }

    internal override ProfileSettingsBase CloneInternal()
    {
        return new TestProfileSettings(LogCodec)
        {
            IsCloned = true
        };
    }

    public bool IsCloned { get; private set; }
    public override string Source => throw new NotImplementedException();
}
