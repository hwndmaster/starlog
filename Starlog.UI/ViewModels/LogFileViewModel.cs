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

    public CollectionViewSource CategoryItemsView { get; } = new();

    public bool IsSelected
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }
}
