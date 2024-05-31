using Genius.Atom.Data.Persistence;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.Models.VersionUpgraders;

public class PlainTextProfileLogCodecVer1To2Upgrader : IDataVersionUpgrader<PlainTextProfileLogCodecV1, PlainTextProfileLogCodecV2>
{
    private readonly Lazy<Settings> _settings;

    public PlainTextProfileLogCodecVer1To2Upgrader(ISettingsQueryService settingsQuery)
    {
        _settings = new Lazy<Settings>(() => settingsQuery.Get());
    }

    public PlainTextProfileLogCodecV2 Upgrade(PlainTextProfileLogCodecV1 value)
    {
        Guard.NotNull(value);

        var foundItem = _settings.Value.PlainTextLogCodecLinePatterns.FirstOrDefault(x => x.Pattern.Equals(value.LineRegex));

        return new PlainTextProfileLogCodecV2
        {
            LogCodec = value.LogCodec,
            LinePatternId = foundItem?.Id ?? _settings.Value.PlainTextLogCodecLinePatterns.First().Id
        };
    }
}
