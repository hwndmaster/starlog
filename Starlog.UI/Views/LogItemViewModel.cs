using System.Windows.Documents;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.UI.Helpers;

namespace Genius.Starlog.UI.Views;

public interface ILogItemViewModel : IViewModel
{
    LogRecord Record { get; }
    bool ColorizeByThread { get; set; }
    string Logger { get; }
    string Message { get; }
    bool IsBookmarked { get; set; }
    FlowDocument Artifacts { get; }
}

public sealed class LogItemViewModel : ViewModelBase, ILogItemViewModel
{
    private readonly Lazy<FlowDocument> _artifactsLazy;

    public LogItemViewModel(LogRecord record, ILogArtifactsFormatter artifactsFormatter)
    {
        Guard.NotNull(artifactsFormatter);

        Record = record.NotNull();
        _artifactsLazy = new Lazy<FlowDocument>(() =>
            artifactsFormatter.CreateArtifactsDocument(Record.File.Artifacts, Record.LogArtifacts));
    }

    public LogRecord Record { get; }

    public DateTimeOffset DateTime => Record.DateTime;
    public string Level => Record.Level.Name;
    public string Thread => Record.Thread;
    public string File => Record.File.FileName;
    public string Logger => Record.Logger.Name;
    public string Message => Record.Message;
    public string? ArtifactsIcon => string.IsNullOrEmpty(Record.LogArtifacts) ? null : "Note32";

    public bool ColorizeByThread
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value, (_, __) => OnPropertyChanged(string.Empty));
    }

    public bool IsBookmarked
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public FlowDocument Artifacts => _artifactsLazy.Value;
}
