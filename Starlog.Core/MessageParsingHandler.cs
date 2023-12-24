using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core;

public interface IMessageParsingHandler
{
    string[] RetrieveColumns(MessageParsing item);
    IEnumerable<string> ParseMessage(MessageParsing item, LogRecord logRecord);
}

internal sealed partial class MessageParsingHandler : IMessageParsingHandler
{
    private ConcurrentDictionary<Guid, string[]> _columnsCache = new();
    private ConcurrentDictionary<Guid, Regex> _regexCache = new();

    public MessageParsingHandler(IEventBus eventBus)
    {
        Guard.NotNull(eventBus);

        eventBus.WhenFired<ProfilesAffectedEvent>()
            .Subscribe(_ =>
            {
                _columnsCache.Clear();
                _regexCache.Clear();
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

    public IEnumerable<string> ParseMessage(MessageParsing item, LogRecord logRecord)
    {
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
                    var count = RetrieveColumns(item).Length;
                    for (var i = 0; i < count; i++)
                        yield return string.Empty;
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