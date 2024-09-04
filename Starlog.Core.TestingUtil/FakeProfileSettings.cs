using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.TestingUtil;

public sealed class FakeProfileSettings : ProfileSettingsBase
{
    public FakeProfileSettings()
        : base(new LogCodec(Guid.NewGuid(), Guid.NewGuid().ToString()))
    {
    }

    public FakeProfileSettings(LogCodec logCodec)
        : base(logCodec)
    {
    }

    internal override ProfileSettingsBase CloneInternal()
    {
        return new FakeProfileSettings(LogCodec)
        {
            IsCloned = true
        };
    }

    public bool IsCloned { get; private set; }
    public override string Source => throw new NotImplementedException();
}
