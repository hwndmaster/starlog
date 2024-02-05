using System.Diagnostics.CodeAnalysis;
using System.Text;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.Helpers;

public interface IClipboardHelper
{
    string CreateLogMessagesStringForClipboard(IEnumerable<ILogItemViewModel> items);
    string CreateLogsStringForClipboard(IEnumerable<ILogItemViewModel> items);
    void CopyToClipboard(string content);
}

// TODO: Cover with unit tests
internal sealed class ClipboardHelper : IClipboardHelper
{
    public string CreateLogMessagesStringForClipboard(IEnumerable<ILogItemViewModel> items)
    {
        return string.Join(Environment.NewLine, items.Select(x => x.Record.Message).Distinct());
    }

    public string CreateLogsStringForClipboard(IEnumerable<ILogItemViewModel> items)
    {
        var itemGroups = items.GroupBy(x => x.Record.Source.DisplayName, x => x.Record);
        StringBuilder sb = new();
        foreach (var itemGroup in itemGroups)
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("---------------------------------------");
                sb.AppendLine();
            }

            sb.Append("File: ").AppendLine(itemGroup.Key);
            foreach (var fileArtifact in itemGroup.First().Source.Artifacts?.Artifacts ?? Array.Empty<string>())
            {
                sb.AppendLine(fileArtifact);
            }

            bool addSpace = false;
            foreach (var item in itemGroup)
            {
                if (addSpace)
                {
                    sb.AppendLine();
                }
                var d = item.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                sb.AppendLine($"{d}\t\t{item.Level.Name}\t{item.Logger.Name}\tThread {item.Thread}\t{item.Message}");

                if (!string.IsNullOrEmpty(item.LogArtifacts))
                {
                    sb.AppendLine(item.LogArtifacts);
                    addSpace = true;
                }
            }
        }

        return sb.ToString();
    }

    [ExcludeFromCodeCoverage]
    public void CopyToClipboard(string content)
    {
        Clipboard.SetText(content);
    }
}
