using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.UI.ViewModels;

public interface ILogItemViewModel
{ }

public sealed class LogItemViewModel : ViewModelBase, ILogItemViewModel
{
    private readonly LogRecord _record;

    public LogItemViewModel(LogRecord record)
    {
        _record = record.NotNull();
    }

    public DateTimeOffset DateTime => _record.DateTime;
    public LogLevel Level => _record.Level;
    public string Thread => _record.Thread;
    // TODO: public string File => _record.File.FileName;
    // TODO: public string FileArtifacts => string.Join(Environment.NewLine, _record.FileArtifacts.Artifacts);
    public string Logger => _record.Logger.Name;
    public string Message => _record.Message;
    // TODO: public string? LogArtifacts => _record.LogArtifacts;
}
