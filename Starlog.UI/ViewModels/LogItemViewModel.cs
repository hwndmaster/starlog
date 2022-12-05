using System.Windows.Documents;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.UI.ViewModels;

public interface ILogItemViewModel : IViewModel
{
    bool ColorizeByThread { get; set; }
    FlowDocument Artifacts { get; }
}

public sealed class LogItemViewModel : ViewModelBase, ILogItemViewModel
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
    // TODO: public string FileArtifacts => string.Join(Environment.NewLine, _record.FileArtifacts.Artifacts);
    public string Logger => Record.Logger.Name;
    public string Message => Record.Message;
    // TODO: public string? LogArtifacts => _record.LogArtifacts;

    public bool ColorizeByThread
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value, (_, __) => base.OnPropertyChanged(""));
    }

    public FlowDocument Artifacts => _artifactsLazy.Value;

    private FlowDocument CreateArtifactsDocument()
    {
        var document = new FlowDocument();

        if (Record.FileArtifacts.Artifacts.Length > 0)
        {
            var para = new Paragraph();
            para.Inlines.Add(new Bold(new Run("File artifacts:\r\n")));
            foreach (var fileArtifact in Record.FileArtifacts.Artifacts)
            {
                para.Inlines.Add(new Run(fileArtifact));
                para.Inlines.Add(new Run(Environment.NewLine));
            }
            document.Blocks.Add(para);
        }

        if (Record.LogArtifacts is not null)
        {
            var para = new Paragraph();
            para.Inlines.Add(new Run(Record.LogArtifacts));
            document.Blocks.Add(para);
        }

        return document;
    }
}
