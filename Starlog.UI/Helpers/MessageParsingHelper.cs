using Genius.Atom.UI.Forms.Controls.AutoGrid;
using Genius.Starlog.Core;
using Genius.Starlog.Core.LogFiltering;

namespace Genius.Starlog.UI.Helpers;

public class MessageParsingHelper(
    IMessageParsingHandler _messageParsingHandler)
{
    public DynamicColumnsViewModel? CreateDynamicMessageParsingEntries(LogRecordFilterContext filterContext, IEnumerable<Views.ILogItemViewModel> logItems)
    {
        Guard.NotNull(filterContext);
        Guard.NotNull(logItems);

        if (filterContext.MessageParsings.Length == 0)
            return null;

        // TODO: Check if there were changes in `filterContext.MessageParsings`
        //       to prevent re-initialization in case of no changes.
        var extractedColumns = (from messageParsing in filterContext.MessageParsings
                                from extractedColumn in _messageParsingHandler.RetrieveColumns(messageParsing)
                                select (messageParsing, extractedColumn)).ToArray();

        foreach (var logItem in logItems)
        {
            logItem.MessageParsingEntries = new DynamicColumnEntriesViewModel(() =>
            {
                List<string> parsedEntriesCombined = [];
                foreach (var messageParsing in filterContext.MessageParsings)
                {
                    var parsedEntries = _messageParsingHandler.ParseMessage(messageParsing, logItem.Record);
                    parsedEntriesCombined.AddRange(parsedEntries);
                }
                return parsedEntriesCombined;
            });
        }

        return new DynamicColumnsViewModel(extractedColumns.Select(x => x.extractedColumn).ToArray());
    }
}
