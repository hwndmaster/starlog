using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.UI.ViewModels;

public interface ILogItemViewModel
{
    bool ColorizeByThread { get; set; }
}

public sealed class LogItemViewModel : ViewModelBase, ILogItemViewModel
{
    public LogItemViewModel(LogRecord record)
    {
        Record = record.NotNull();
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
}
