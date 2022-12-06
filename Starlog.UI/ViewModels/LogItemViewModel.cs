using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.UI.ViewModels;

public interface ILogItemViewModel : IViewModel
{
    bool ColorizeByThread { get; set; }
    FlowDocument Artifacts { get; }
}

public sealed partial class LogItemViewModel : ViewModelBase, ILogItemViewModel
{
    private readonly Lazy<FlowDocument> _artifactsLazy;

    public LogItemViewModel(LogRecord record)
    {
        Record = record.NotNull();
        _artifactsLazy = new Lazy<FlowDocument>(() => CreateArtifactsDocument());
    }

    public LogRecord Record { get; }

    public DateTimeOffset DateTime => Record.DateTime;
    public string Level => Record.Level.Name;
    public string Thread => Record.Thread;
    public string File => Record.File.FileName;
    public string Logger => Record.Logger.Name;
    public string Message => Record.Message;

    public bool ColorizeByThread
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value, (_, __) => OnPropertyChanged(""));
    }

    public FlowDocument Artifacts => _artifactsLazy.Value;

    private FlowDocument CreateArtifactsDocument()
    {
        var document = new FlowDocument();

        if (Record.FileArtifacts.Artifacts.Length > 0)
        {
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(new Bold(new Run("File artifacts:\r\n")));
            foreach (var fileArtifact in Record.FileArtifacts.Artifacts)
            {
                AddFormatted(paragraph, fileArtifact);
                paragraph.Inlines.Add(new Run(Environment.NewLine));
            }
            document.Blocks.Add(paragraph);
        }

        if (!string.IsNullOrEmpty(Record.LogArtifacts))
        {
            var paragraph = new Paragraph();
            AddFormatted(paragraph, Record.LogArtifacts);
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
                brush = Brushes.DeepPink;
            }
            else if (match.Groups["exc"].Success)
            {
                brush = Brushes.IndianRed;
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

    [GeneratedRegex("(?<str>(?<!\\w)'[^']+')|(?<str2>(?<!\\w)\"[^\"]+\")|(?<num>(?<!\\w)[\\d\\.,]*\\d(?!\\w))|(?<at>[ ]{3}at\\s.+)|(?<exc>\\w+Exception)")]
    private static partial Regex FormattedStringRegex();
}
