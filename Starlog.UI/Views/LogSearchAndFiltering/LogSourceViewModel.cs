using System.Windows.Data;
using Genius.Starlog.Core.LogFlow;

namespace Genius.Starlog.UI.Views.LogSearchAndFiltering;

public sealed class LogSourceViewModel : ViewModelBase, ILogFilterNodeViewModel
{
    public LogSourceViewModel(LogSourceBase source)
    {
        Source = source.NotNull();
        AddChildCommand = new ActionCommand(_ => throw new NotSupportedException());
        PinCommand = new ActionCommand(_ => IsPinned = !IsPinned);
    }

    internal void HandleSourceRenamed(LogSourceBase newRecord)
    {
        Source = newRecord;
        Title = Source.DisplayName;
    }

    public LogSourceBase Source { get; private set; }
    public string Title
    {
        get => GetOrDefault(Source.DisplayName);
        set => RaiseAndSetIfChanged(value);
    }

    public string Icon => "LogFile32";
    public bool CanAddChildren => false;
    public bool CanModifyOrDelete => false;
    public bool CanPin => true;
    public bool IsExpanded { get; set; }

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
