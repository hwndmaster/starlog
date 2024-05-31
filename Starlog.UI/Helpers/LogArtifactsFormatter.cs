using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.UI.Helpers;

public interface ILogArtifactsFormatter
{
    FlowDocument CreateArtifactsDocument(SourceArtifacts? fileArtifacts, string? logArtifacts);
}

public sealed partial class LogArtifactsFormatter : ILogArtifactsFormatter
{
    public FlowDocument CreateArtifactsDocument(SourceArtifacts? fileArtifacts, string? logArtifacts)
    {
        var document = new FlowDocument();

        if (fileArtifacts?.Artifacts.Length > 0)
        {
            var paragraph = new Paragraph()
            {
                TextAlignment = TextAlignment.Left
            };
            paragraph.Inlines.Add(new Italic(new Run("File artifacts:\r\n")));
            foreach (var fileArtifact in fileArtifacts.Artifacts)
            {
                AddFormatted(paragraph, fileArtifact);
                paragraph.Inlines.Add(new LineBreak());
            }
            document.Blocks.Add(paragraph);
        }

        if (!string.IsNullOrEmpty(logArtifacts))
        {
            var paragraph = new Paragraph();
            AddFormatted(paragraph, logArtifacts);
            document.Blocks.Add(paragraph);
        }

        return document;
    }

    private static void AddFormatted(Paragraph paragraph, string text)
    {
        int indexFrom = 0;
        var matches = FormattedStringRegex().Matches(text);
        foreach (Match match in matches)
        {
            if (indexFrom != match.Index)
            {
                var plainText = text[indexFrom..match.Index];
                paragraph.Inlines.Add(new Run(plainText));
            }

            Brush brush = Brushes.Black;
            if (match.Groups["str"].Success || match.Groups["str2"].Success)
            {
                brush = Brushes.ForestGreen;
            }
            else if (match.Groups["num"].Success)
            {
                brush = Brushes.Cyan;
            }
            else if (match.Groups["exc"].Success)
            {
                brush = Brushes.IndianRed;
            }
            else if (match.Groups["date"].Success)
            {
                brush = Brushes.GreenYellow;
            }
            else if (match.Groups["url"].Success)
            {
                brush = Brushes.HotPink;
            }
            else if (match.Groups["at"].Success)
            {
                brush = Brushes.Gray;
            }

            paragraph.Inlines.Add(new Run(match.Value) { Foreground = brush });

            indexFrom = match.Index + match.Length;
            if (match.Value[^1] == '\r')
                indexFrom++;
        }

        if (indexFrom < text.Length)
        {
            var plainText = text[indexFrom..];
            paragraph.Inlines.Add(new Run(plainText));
        }
    }

    [GeneratedRegex("(?<date>(\\d{4}-\\d{2}-\\d{2})|(\\d{2}-\\d{2}-\\d{4})|(\\d+\\s\\w{3,}\\s\\d{4})|(\\d{2}:\\d{2}:\\d{2}(\\.\\d{3})?))|(?<str>(?<!\\w)'[^']+')|(?<str2>(?<!\\w)\"[^\"]+\")|(?<num>(?<!\\w)[\\d\\.,]*\\d(?!\\w))|(?<at>[ ]{3}at\\s.+)|(?<exc>\\w+Exception)|(?<url>\\w+:(\\/\\/|\\\\)[\\w\\.\\/\\\\\\?=_+&%~@#\\(\\)]+)")]
    private static partial Regex FormattedStringRegex();
}
