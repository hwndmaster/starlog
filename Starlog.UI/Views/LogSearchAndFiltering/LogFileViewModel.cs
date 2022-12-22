using System.Windows.Data;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.UI.Views.LogSearchAndFiltering;

public sealed class LogFileViewModel : ViewModelBase, ILogFilterNodeViewModel
{
    public LogFileViewModel(FileRecord file)
    {
        File = file.NotNull();
        AddChildCommand = new ActionCommand(_ => throw new NotSupportedException());
        PinCommand = new ActionCommand(_ => IsPinned = !IsPinned);
    }

    public FileRecord File { get; }
    public string Title => File.FileName;
    public string Icon => "LogFile32";
    public bool CanAddChildren => false;
    public bool CanModifyOrDelete => false;
    public bool CanPin => true;
    public bool IsExpanded { get; set; } = false;

    public bool IsPinned
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public CollectionViewSource CategoryItemsView { get; } = new();
    public IActionCommand AddChildCommand { get; }
    public IActionCommand ModifyCommand => throw new NotSupportedException();
    public IActionCommand DeleteCommand => throw new NotSupportedException();
    public IActionCommand PinCommand { get; }
}
