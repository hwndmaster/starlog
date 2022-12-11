using System.Windows.Data;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.UI.ViewModels;

public sealed class LogFileViewModel : ViewModelBase, ILogFilterNodeViewModel
{
    public LogFileViewModel(FileRecord file)
    {
        File = file.NotNull();
    }

    public FileRecord File { get; }
    public string Title => File.FileName;
    public string Icon => "LogFile32";
    public bool CanAddChildren => false;
    public bool CanModifyOrDelete => false;
    public bool IsExpanded { get; set; } = false;

    public CollectionViewSource CategoryItemsView { get; } = new();
    public IActionCommand AddChildCommand => throw new NotSupportedException();
    public IActionCommand ModifyCommand => throw new NotSupportedException();
    public IActionCommand DeleteCommand => throw new NotSupportedException();
}
