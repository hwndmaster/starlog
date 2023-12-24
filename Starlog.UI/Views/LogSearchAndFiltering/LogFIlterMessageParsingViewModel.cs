using System.Windows.Data;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.LogSearchAndFiltering;

// TODO: Cover with unit tests
public sealed class LogFilterMessageParsingViewModel : ViewModelBase, ILogFilterNodeViewModel,
    IHasModifyCommand, IHasDeleteCommand, ISelectable
{
    public LogFilterMessageParsingViewModel(MessageParsing messageParsing, bool isUserDefined)
    {
        // Members initialization:
        MessageParsing = messageParsing.NotNull();
        IsUserDefined = isUserDefined;
        Icon = "MessageParsing32";

        // Actions:
        AddChildCommand = new ActionCommand(_ => throw new NotSupportedException());

        if (isUserDefined)
        {
            ModifyCommand = new ActionCommand();
            DeleteCommand = new ActionCommand();
        }
        else
        {
            ModifyCommand = new ActionCommand(_ => throw new NotSupportedException());
            DeleteCommand = new ActionCommand(_ => throw new NotSupportedException());
        }

        PinCommand = new ActionCommand(_ => IsPinned = !IsPinned);
    }

    internal void Reconcile()
    {
        OnPropertyChanged(nameof(Title));
    }

    public MessageParsing MessageParsing { get; }
    public string Title => MessageParsing.Name;
    public string Icon { get; }
    public bool IsUserDefined { get; }
    public bool CanAddChildren => false;
    public bool CanModifyOrDelete => IsUserDefined;
    public bool CanPin => true;
    public bool IsExpanded { get; set; } = false;

    public bool IsPinned
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public bool IsSelected
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public CollectionViewSource CategoryItemsView { get; } = new();

    public IActionCommand AddChildCommand { get; }
    public IActionCommand ModifyCommand { get; }
    public IActionCommand DeleteCommand { get; }
    public IActionCommand PinCommand { get; }
}
