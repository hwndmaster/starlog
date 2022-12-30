using System.Text;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.Helpers;

public static class CopyToClipboardHelper
{
    public static string CreateLogsStringForClipboard(IEnumerable<ILogItemViewModel> items)
    {
        var itemGroups = items.OfType<ILogItemViewModel>()
            .GroupBy(x => x.Record.File.FileName, x => x.Record);
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
            foreach (var fileArtifact in itemGroup.First().File.Artifacts?.Artifacts ?? Array.Empty<string>())
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
                sb.AppendLine($"{d}\t\t{item.Level}\t{item.Logger}\tThread {item.Thread}\t{item.Message}");

                if (!string.IsNullOrEmpty(item.LogArtifacts))
                {
                    sb.AppendLine(item.LogArtifacts);
                    addSpace = true;
                }
            }
        }

        return sb.ToString();
    }

    public static void CopyToClipboard(string content)
    {
        Clipboard.SetText(content);
    }
}
