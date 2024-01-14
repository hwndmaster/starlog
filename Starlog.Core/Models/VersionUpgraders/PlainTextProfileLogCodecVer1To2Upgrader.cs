using Genius.Atom.Data.Persistence;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.Models.VersionUpgraders;

public class PlainTextProfileLogCodecVer1To2Upgrader : IDataVersionUpgrader<PlainTextProfileLogCodecLegacy, PlainTextProfileLogCodec>
{
    private readonly Lazy<Settings> _settings;

    public PlainTextProfileLogCodecVer1To2Upgrader(ISettingsQueryService settingsQuery)
    {
        _settings = new Lazy<Settings>(() => settingsQuery.Get());
    }

    public PlainTextProfileLogCodec Upgrade(PlainTextProfileLogCodecLegacy value)
    {
        var foundItem = _settings.Value.PlainTextLogCodecLinePatterns.FirstOrDefault(x => x.Pattern.Equals(value.LineRegex));

        return new PlainTextProfileLogCodec(value.LogCodec)
        {
            LinePatternId = foundItem?.Id ?? _settings.Value.PlainTextLogCodecLinePatterns.First().Id
        };
    }
}
