using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core;

public interface IMessageParsingHandler
{
    string[] RetrieveColumns(MessageParsing item);
    IEnumerable<string> ParseMessage(MessageParsing item, LogRecord logRecord, bool testingMode = false);
}

internal sealed partial class MessageParsingHandler : IMessageParsingHandler
{
    private ConcurrentDictionary<Guid, string[]> _columnsCache = new();
    private ConcurrentDictionary<Guid, Regex> _regexCache = new();
    private ConcurrentDictionary<Guid, (ProfileFilterBase?, IFilterProcessor?)> _filterCache = new();

    private readonly ILogFilterContainer _logFilterContainer;
    private readonly ICurrentProfile _currentProfile;
    private readonly IQuickFilterProvider _quickFilterProvider;

    public MessageParsingHandler(
        ICurrentProfile currentProfile,
        IEventBus eventBus,
        ILogFilterContainer logFilterContainer,
        IQuickFilterProvider quickFilterProvider)
    {
        Guard.NotNull(eventBus);
        _currentProfile = currentProfile.NotNull();
        _logFilterContainer = logFilterContainer.NotNull();
        _quickFilterProvider = quickFilterProvider.NotNull();

        eventBus.WhenFired<ProfilesAffectedEvent>()
            .Subscribe(_ =>
            {
                _columnsCache.Clear();
                _regexCache.Clear();
                _filterCache.Clear();
            });
    }

    public string[] RetrieveColumns(MessageParsing item)
    {
        switch (item.Method)
        {
            case MessageParsingMethod.RegEx:
            {
                return _columnsCache.GetOrAdd(item.Id, (_) =>
                {
                    var regex = EntriesRegex();
                    return regex.Matches(item.Pattern).Select(x => x.Groups["Entry"].Value).ToArray();
                });
            }
            default:
                throw new NotSupportedException($"Method {item.Method} is not currently supported.");
        }
    }

    public IEnumerable<string> ParseMessage(MessageParsing item, LogRecord logRecord, bool testingMode = false)
    {
        if (item.Filters is not null && item.Filters.Length > 0
            && _currentProfile.Profile is not null)
        {
            foreach (var filterId in item.Filters)
            {
                var (filter, processor) = _filterCache.GetOrAdd(filterId, _ =>
                {
                    var filter = _currentProfile.Profile!.Filters
                        .Concat(_quickFilterProvider.GetQuickFilters())
                        .FirstOrDefault(x => x.Id == filterId);
                    var processor = filter is null ? null : _logFilterContainer.GetFilterProcessor(filter);
                    return (filter, processor);
                });

                if (filter is not null && processor is not null
                    && !processor.IsMatch(filter, logRecord))
                {
                    if (!testingMode)
                    {
                        var count = RetrieveColumns(item).Length;
                        for (var i = 0; i < count; i++)
                            yield return string.Empty;
                    }
                    yield break;
                }
            }
        }

        switch (item.Method)
        {
            case MessageParsingMethod.RegEx:
            {
                var regex = GetRegex(item);
                var match = regex.Match(logRecord.Message);
                if (!match.Success && logRecord.LogArtifacts is not null)
                {
                    match = regex.Match(logRecord.LogArtifacts);
                }
                if (!match.Success)
                {
                    if (!testingMode)
                    {
                        var count = RetrieveColumns(item).Length;
                        for (var i = 0; i < count; i++)
                            yield return string.Empty;
                    }
                    break;
                }

                for (var i = 1; i < match.Groups.Count; i++)
                    yield return match.Groups[i].Value;

                break;
            }
            default:
                throw new NotSupportedException($"Method {item.Method} is not currently supported.");
        }
    }

    private Regex GetRegex(MessageParsing item)
    {
        return _regexCache.GetOrAdd(item.Id, (_)
            => new Regex(item.Pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase));
    }

    [GeneratedRegex(@"\(\?<(?<Entry>\w+)>")]
    private static partial Regex EntriesRegex();
}