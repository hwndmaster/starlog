using System.Windows.Data;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.UI.ViewModels;

public sealed class LogFileViewModel : ViewModelBase, ILogFilterCategoryViewModel
{
    public LogFileViewModel(FileRecord file)
    {
        File = file.NotNull();
    }

    public FileRecord File { get; }
    public string Title => File.FileName;
    public string Icon => "LogFile32";
    public bool IsExpanded { get; set; } = false;

    public CollectionViewSource CategoryItemsView { get; } = new();
}